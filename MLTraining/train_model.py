import ast
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

EXCLUDED_DISEASES = {
    "benign prostatic hyperplasia (bph)",
    "cystitis",
    "hyperemesis gravidarum",
    "idiopathic excessive menstruation",
    "idiopathic irregular menstrual cycle",
    "idiopathic painful menstruation",
    "pelvic inflammatory disease",
    "problem during pregnancy",
    "spontaneous abortion",
    "temporary or benign blood in urine",
    "threatened pregnancy",
    "urinary tract infection",
    "vaginal cyst",
    "vaginitis",
    "vulvodynia",
}

training = pd.read_csv(f"{DATA_DIR}/Diseases_and_Symptoms_dataset.csv")
training = training[~training["diseases"].isin(EXCLUDED_DISEASES)].reset_index(drop=True)

symptom_columns = [c for c in training.columns if c != "diseases"]

print(f"Rows: {len(training)}")
print(f"Symptom columns: {len(symptom_columns)}")
print(f"Unique diseases: {training['diseases'].nunique()}")

label_encoder = LabelEncoder()
y = label_encoder.fit_transform(training["diseases"])
X = training[symptom_columns].astype("float32")

X_train, X_test, y_train, y_test = train_test_split(
    X, y, test_size=0.2, random_state=42, stratify=y
)

print(f"Training rows: {len(X_train)}")
print(f"Test rows: {len(X_test)}")

def augment_partial_and_noisy(X, y, n_augment_per_class=150, drop_prob=0.7,
                               max_drop_fraction=0.35, noise_prob=0.2,
                               max_noise_symptoms=2, random_state=42):
    rng = np.random.RandomState(random_state)
    X_arr = X.values if hasattr(X, "values") else X

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


X_aug, y_aug = augment_partial_and_noisy(X_train, y_train, random_state=42)
X_train_full = np.vstack([X_train.values, X_aug])
y_train_full = np.concatenate([y_train, y_aug])
print(f"Training rows: {len(X_train)} clean + {len(X_aug)} augmented = {len(X_train_full)}")

model = RandomForestClassifier(n_estimators=200, max_depth=20, min_samples_leaf=2, random_state=42)
model.fit(X_train_full, y_train_full)

accuracy = accuracy_score(y_test, model.predict(X_test))
print(f"Clean holdout accuracy: {accuracy:.4f}")

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


def build_disease_info():
    description_df = pd.read_csv(f"{DATA_DIR}/description.csv")
    medications_df = pd.read_csv(f"{DATA_DIR}/medications.csv")
    precautions_df = pd.read_csv(f"{DATA_DIR}/precautions.csv")

    description_map = {
        row["Disease"].strip().lower(): row["Description"]
        for _, row in description_df.iterrows()
    }

    medication_map = {}
    for _, row in medications_df.iterrows():
        try:
            meds = ast.literal_eval(row["Medication"])
        except (ValueError, SyntaxError):
            meds = [row["Medication"]]
        medication_map[row["Disease"].strip().lower()] = meds

    precaution_cols = [c for c in precautions_df.columns if c != "Disease"]
    precaution_map = {}
    for _, row in precautions_df.iterrows():
        precautions = [row[c] for c in precaution_cols if pd.notna(row[c]) and str(row[c]).strip()]
        precaution_map[row["Disease"].strip().lower()] = precautions

    disease_info = {}
    for disease in label_encoder.classes_:
        disease_info[disease] = {
            "Description": description_map.get(disease, ""),
            "Precautions": precaution_map.get(disease, []),
            "Medications": medication_map.get(disease, []),
        }

    matched = sum(1 for d in disease_info.values() if d["Description"])
    print(f"Disease info matched: {matched}/{len(disease_info)}")

    return disease_info


disease_info = build_disease_info()
with open(f"{OUT_DIR}/disease_info.json", "w") as f:
    json.dump(disease_info, f, indent=2)
print(f"Saved disease info to {OUT_DIR}/disease_info.json")