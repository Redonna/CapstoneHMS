"""
Trains a symptom -> disease classifier on the Kaggle-style disease/symptom
dataset and exports it to ONNX so it can be loaded in-process by the .NET API
(no live Python service). Also builds a disease-info lookup (description,
precautions, medications) consumed by the API for the "suggested treatment"
part of the summary.

One-time offline step. Re-run only if the dataset or model choice changes.
"""

import json
import re

import numpy as np
import pandas as pd
from sklearn.ensemble import RandomForestClassifier
from sklearn.model_selection import train_test_split
from sklearn.preprocessing import LabelEncoder
from sklearn.metrics import accuracy_score
from skl2onnx import convert_sklearn
from skl2onnx.common.data_types import FloatTensorType
import onnx

DATA_DIR = "data"
OUT_DIR = "../HospitalManagementSystem.API/MLModels"


DISEASE_NAME_ALIASES = {
    # Known typo in Training.csv / precautions_df.csv vs description.csv / medications.csv
    "Peptic ulcer diseae": "Peptic ulcer disease",
}


def normalize_disease_name(name: str) -> str:
    cleaned = re.sub(r"\s+", " ", str(name).strip())
    return DISEASE_NAME_ALIASES.get(cleaned, cleaned)


def augment_partial_and_noisy(X, y, n_augment_per_class=150, drop_prob=0.7,
                               max_drop_fraction=0.35, noise_prob=0.2,
                               max_noise_symptoms=2, random_state=42):
    """
    Generates synthetic training rows that simulate real-world messiness:
    - "partial" copies with a few of the disease's true symptoms dropped
      (a doctor/patient rarely reports every symptom of a condition)
    - "noisy" copies with 1-2 unrelated symptoms added
      (mirrors this app's own use case: blending a patient's past-history
      symptoms with newly-described current symptoms, which may not belong
      to the same condition)
    Trains the model to still favor the dominant disease pattern under both
    kinds of real-world noise, instead of only ever seeing perfectly clean
    textbook symptom sets.
    """
    rng = np.random.RandomState(random_state)
    X_arr = X.values if hasattr(X, "values") else X
    n_symptoms = X_arr.shape[1]

    augmented_X, augmented_y = [], []

    for class_idx in np.unique(y):
        patterns = np.unique(X_arr[y == class_idx], axis=0)

        for _ in range(n_augment_per_class):
            row = patterns[rng.randint(len(patterns))].copy()
            true_indices = np.where(row == 1)[0]

            if len(true_indices) > 2 and rng.rand() < drop_prob:
                max_drop = max(1, int(len(true_indices) * max_drop_fraction))
                n_drop = rng.randint(1, max_drop + 1)
                n_drop = min(n_drop, len(true_indices) - 2)
                if n_drop > 0:
                    drop_indices = rng.choice(true_indices, size=n_drop, replace=False)
                    row[drop_indices] = 0

            if rng.rand() < noise_prob:
                zero_indices = np.where(row == 0)[0]
                n_noise = rng.randint(1, max_noise_symptoms + 1)
                if len(zero_indices) >= n_noise:
                    noise_indices = rng.choice(zero_indices, size=n_noise, replace=False)
                    row[noise_indices] = 1

            augmented_X.append(row)
            augmented_y.append(class_idx)

    return np.array(augmented_X, dtype="float32"), np.array(augmented_y)


def main():
    training = pd.read_csv(f"{DATA_DIR}/Training.csv")
    # Drop any unnamed/empty trailing column some mirrors of this dataset have
    training = training.loc[:, ~training.columns.str.contains(r"^Unnamed")]

    symptom_columns = [c for c in training.columns if c != "prognosis"]
    training["prognosis"] = training["prognosis"].map(normalize_disease_name)

    X = training[symptom_columns].fillna(0).astype("float32")
    y_raw = training["prognosis"]

    label_encoder = LabelEncoder()
    y = label_encoder.fit_transform(y_raw)

    X_train, X_test, y_train, y_test = train_test_split(
        X, y, test_size=0.2, random_state=42, stratify=y
    )

    # Augment only the training split (never the held-out test data) with
    # partial/noisy variants so the model learns robustness to real-world
    # symptom reporting instead of only clean textbook patterns.
    X_aug, y_aug = augment_partial_and_noisy(X_train, y_train, random_state=42)
    X_train_full = np.vstack([X_train.values, X_aug])
    y_train_full = np.concatenate([y_train, y_aug])
    print(f"Training rows: {len(X_train)} clean + {len(X_aug)} augmented = {len(X_train_full)}")

    model = RandomForestClassifier(n_estimators=300, random_state=42)
    model.fit(X_train_full, y_train_full)

    accuracy = accuracy_score(y_test, model.predict(X_test))
    print(f"Clean holdout accuracy: {accuracy:.4f}")

    # A second, more realistic metric: how well the model handles partial/noisy
    # symptom reports it was never trained on (built from the held-out test split).
    X_noisy_test, y_noisy_test = augment_partial_and_noisy(
        X_test, y_test, n_augment_per_class=30, random_state=99
    )
    noisy_accuracy = accuracy_score(y_noisy_test, model.predict(X_noisy_test))
    print(f"Partial/noisy holdout accuracy: {noisy_accuracy:.4f}")

    onnx_model = convert_sklearn(
        model,
        initial_types=[("symptoms", FloatTensorType([None, len(symptom_columns)]))],
        options={id(model): {"zipmap": False}},
        target_opset=15,
    )
    onnx.save_model(onnx_model, f"{OUT_DIR}/symptom_model.onnx")
    print(f"Saved ONNX model to {OUT_DIR}/symptom_model.onnx")

    with open(f"{OUT_DIR}/symptom_features.json", "w") as f:
        json.dump(symptom_columns, f, indent=2)

    with open(f"{OUT_DIR}/disease_labels.json", "w") as f:
        json.dump(list(label_encoder.classes_), f, indent=2)

    build_disease_info(list(label_encoder.classes_))


def build_disease_info(diseases: list[str]):
    description = pd.read_csv(f"{DATA_DIR}/description.csv")
    precautions = pd.read_csv(f"{DATA_DIR}/precautions_df.csv")
    medications = pd.read_csv(f"{DATA_DIR}/medications.csv")

    description["Disease"] = description["Disease"].map(normalize_disease_name)
    precautions["Disease"] = precautions["Disease"].map(normalize_disease_name)
    medications["Disease"] = medications["Disease"].map(normalize_disease_name)

    desc_by_disease = dict(zip(description["Disease"], description["Description"]))

    precaution_cols = ["Precaution_1", "Precaution_2", "Precaution_3", "Precaution_4"]
    precautions_by_disease = {}
    for _, row in precautions.iterrows():
        items = [row[c] for c in precaution_cols if pd.notna(row[c]) and str(row[c]).strip()]
        precautions_by_disease[row["Disease"]] = items

    med_by_disease = {}
    for _, row in medications.iterrows():
        raw = row["Medication"]
        try:
            items = json.loads(str(raw).replace("'", '"'))
        except (json.JSONDecodeError, TypeError):
            items = [raw] if pd.notna(raw) else []
        med_by_disease[row["Disease"]] = items

    info = {}
    missing = []
    for disease in diseases:
        if disease not in desc_by_disease:
            missing.append(disease)
        info[disease] = {
            "description": desc_by_disease.get(disease, ""),
            "precautions": precautions_by_disease.get(disease, []),
            "medications": med_by_disease.get(disease, []),
        }

    if missing:
        print(f"WARNING: no description/precaution/medication data for: {missing}")

    with open(f"{OUT_DIR}/disease_info.json", "w") as f:
        json.dump(info, f, indent=2)
    print(f"Saved disease info lookup to {OUT_DIR}/disease_info.json")


if __name__ == "__main__":
    main()
