import os
import json

brain_dir = r"C:\Users\Upeka\.gemini\antigravity-ide\brain"
for folder in os.listdir(brain_dir):
    meta_path = os.path.join(brain_dir, folder, "metadata.json")
    if os.path.exists(meta_path):
        with open(meta_path, 'r', encoding='utf-8') as f:
            try:
                meta = json.load(f)
                print(f"Folder: {folder}")
                print(f"  Summary: {meta.get('summary', 'No summary')}")
                print(f"  Task: {meta.get('task', 'No task')}")
            except Exception as e:
                print(f"Folder {folder}: error reading meta: {e}")
    else:
        # Check subfolders
        pass
