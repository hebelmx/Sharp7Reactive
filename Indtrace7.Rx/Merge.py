import os

def merge_cs_files(project_root):
    merged_code = []

    # Calculate the parent directory for output
    parent_dir = os.path.abspath(os.path.join(project_root, os.pardir))
    os.makedirs(parent_dir, exist_ok=True)  # Ensure the parent directory exists
    output_path = os.path.join(parent_dir, "MergedProject.cs")

    for dirpath, _, filenames in os.walk(project_root):
        if 'bin' in dirpath or 'obj' in dirpath:
            continue

        for filename in filenames:
            if filename.endswith(".cs") and not filename.endswith(".Designer.cs"):
                full_path = os.path.join(dirpath, filename)
                if os.path.abspath(full_path) == os.path.abspath(output_path):
                    continue  # Prevent recursive inclusion

                with open(full_path, 'r', encoding='utf-8') as file:
                    merged_code.append(f"// ===== File: {filename} =====")
                    merged_code.append(file.read())
                    merged_code.append("\n\n")

    with open(output_path, 'w', encoding='utf-8') as out_file:
        out_file.write("\n".join(merged_code))

    print(f"✅ Merged code saved to: {output_path}")

# Replace this with your actual path
project_folder = r"C:\Users\Abel Briones\Documents\GitHub\Sharp7Reactive\Indtrace7.Rx"
merge_cs_files(project_folder)
