"""
upload_dataset.py
=================
Automated script to publish the Architectural Surrogate Regression Dataset
to the Hugging Face Hub under 'vykrum/arch-regression-dataset'.

Prerequisites:
  pip install huggingface_hub pyarrow pandas

Usage:
  huggingface-cli login
  python upload_dataset.py
"""

import os
import sys

try:
    from huggingface_hub import HfApi, create_repo
except ImportError:
    print("Please install huggingface_hub: pip install huggingface_hub")
    sys.exit(1)

REPO_ID = "vykrum/arch-regression-dataset"
DATASET_DIR = os.path.dirname(os.path.abspath(__file__))


def main():
    print(f"[*] Preparing to upload dataset from '{DATASET_DIR}' to Hugging Face Hub: {REPO_ID}")
    api = HfApi()

    try:
        # Create repo if it doesn't already exist
        create_repo(repo_id=REPO_ID, repo_type="dataset", exist_ok=True)
        print(f"[+] Dataset repository verified on Hugging Face: https://huggingface.co/datasets/{REPO_ID}")

        # Upload files
        api.upload_folder(
            folder_path=DATASET_DIR,
            repo_id=REPO_ID,
            repo_type="dataset",
            ignore_patterns=["upload_dataset.py", "__pycache__/*", "*.pyc"]
        )
        print("[+] Upload complete! Dataset is live on Hugging Face.")
    except Exception as e:
        print(f"[-] Error uploading to Hugging Face Hub: {e}")
        print("    Ensure you have authenticated via 'huggingface-cli login' or set HF_TOKEN environment variable.")


if __name__ == "__main__":
    main()
