"""
Seeds the 5 generated demo patients into the running API and uploads each
patient's Word-document medical history as an attachment on a history entry.
Run only after the API is up and generate_demo_histories.py has produced
demo_output/*.docx + manifest.json.
"""

import json
import requests

BASE = "http://localhost:56076/api"
OUT_DIR = "demo_output"


def login(username, password):
    res = requests.post(f"{BASE}/auth/login", json={"username": username, "password": password})
    res.raise_for_status()
    return res.json()["token"]


def main():
    token = login("admin", "Admin@123")
    headers = {"Authorization": f"Bearer {token}"}

    manifest = json.load(open(f"{OUT_DIR}/manifest.json"))

    for patient in manifest:
        body = {
            "firstName": patient["firstName"],
            "lastName": patient["lastName"],
            "dateOfBirth": f"{patient['dob']}T00:00:00",
            "gender": patient["gender"],
            "phoneNumber": patient["phone"],
            "email": patient["email"],
            "address": patient["address"],
        }
        res = requests.post(f"{BASE}/patients", json=body, headers=headers)
        res.raise_for_status()
        patient_id = res.json()["id"]
        print(f"Created patient {patient['firstName']} {patient['lastName']} (id={patient_id})")

        entry_body = {
            "patientId": patient_id,
            "title": "Full Medical History",
            "details": f"Consolidated medical history document covering: {', '.join(patient['diseases'])}.",
        }
        res = requests.post(f"{BASE}/patienthistory", json=entry_body, headers=headers)
        res.raise_for_status()
        entry_id = res.json()["id"]

        docx_path = f"{OUT_DIR}/{patient['docxFile']}"
        with open(docx_path, "rb") as f:
            files = {"file": (patient["docxFile"], f,
                               "application/vnd.openxmlformats-officedocument.wordprocessingml.document")}
            res = requests.post(f"{BASE}/patienthistory/{entry_id}/attachment", files=files, headers=headers)
        res.raise_for_status()
        print(f"  Uploaded {patient['docxFile']} as attachment on history entry {entry_id}")

    print("\nDone seeding 5 demo patients with medical history documents.")


if __name__ == "__main__":
    main()
