import subprocess
import shutil
import os
import xml.etree.ElementTree as ET

def create_dotnet_project(project_name):
    subprocess.run(['dotnet', 'new', 'webapi', '-n', project_name, '--force', '--framework', 'net6.0'], check=True)
    print("Project created.")

def copy_code(project_name, source_folder):
    source_directory = source_folder  # Path to the 'code_output' directory
    destination_directory = os.path.join(project_name)  # Path to the 'ProjectName' directory

    for item in os.listdir(source_directory):  # List each file or directory in the source folder
        s_item = os.path.join(source_directory, item)  # Full path of source item
        d_item = os.path.join(destination_directory, item)  # Full path of destination item

        if os.path.isdir(s_item):
            shutil.copytree(s_item, d_item, dirs_exist_ok=True)  # Recursively copy directories
        else:
            shutil.copy2(s_item, d_item)  # Copy files

    print(f"All contents from {source_folder} copied to {project_name}.")


def clean_project(project_name):
    # Remove WeatherForecast files
    for root, dirs, files in os.walk(project_name):
        for file in files:
            if 'WeatherForecast' in file:
                os.remove(os.path.join(root, file))
    # Update .csproj file
    csproj_file = os.path.join(project_name, f"{project_name}.csproj")
    tree = ET.parse(csproj_file)
    root = tree.getroot()
    for item_group in root.findall('ItemGroup'):
        for element in list(item_group):
            if 'WeatherForecast' in element.attrib.get('Include', ''):
                item_group.remove(element)
    tree.write(csproj_file)
    print("Project cleaned from WeatherForecast references.")

def find_missing_packages(project_name):
    missing_packages = set()

    # Known system and common namespaces that don't typically require additional packages
    known_namespaces = {'System', f'{project_name}'}

    # Capture the output from grep
    result = subprocess.run(['grep', '-R', '^using', project_name], capture_output=True, text=True)
    
    # print("Grep output:")
    # print(result.stdout)  # Let's print this again to ensure our input is correct.

    for line in result.stdout.splitlines():
        parts = line.split()
        if len(parts) > 1:  # Ensure there is a namespace to process
            namespace = parts[1].rstrip(';')
            
            # Debug print to see what namespaces are being processed
            # print(f"Processing namespace: {namespace}")

            # Check if the namespace is not a known system or project namespace
            if not namespace == 'System' and not namespace.startswith('System.') and \
               not any(namespace.startswith(ns + '.') for ns in known_namespaces):
                missing_packages.add(namespace)

    return list(missing_packages)






def install_packages(project_name, packages):
    for package in packages:
        # Here, add logic to determine the correct version compatible with .NET 6
        subprocess.run(['dotnet', 'add', project_name, 'package', package], check=True)
    print("Packages installed.")






def compile_and_log_errors(project_folder):
    backlog_folder = os.path.join(project_folder, 'backlog')
    os.makedirs(backlog_folder, exist_ok=True)

    project_file = None
    for file in os.listdir(project_folder):
        if file.endswith(".csproj"):
            project_file = os.path.join(project_folder, file)
            break

    if not project_file:
        print("No .csproj file found in the project directory.")
        return

    build_log_path = os.path.join(backlog_folder, 'build_log.txt')
    build_error_log_path = os.path.join(backlog_folder, 'build_error_log.txt')

    with open(build_log_path, 'w') as build_log:
        completed_process = subprocess.run(['dotnet', 'build', project_file], stdout=build_log, stderr=subprocess.STDOUT)

    if completed_process.returncode == 0:
        print('Build successful! No errors found.')
    else:
        print('Build failed! Check build logs for errors.')

    # Initialize a set to store unique error messages
    unique_errors = set()

    # Extract and format error information
    with open(build_log_path, 'r') as log_file:
        for line in log_file:
            if 'error' in line.lower():
                # Format the error message
                formatted_error = line.split('[')[0].strip()  # Remove the project path part
                formatted_error = f"Error: {formatted_error}\n"  # Add "Error: " prefix
                unique_errors.add(formatted_error)

    # Write unique error messages to the error log file
    with open(build_error_log_path, 'w') as error_log_file:
        for error in sorted(unique_errors):  # Optional sorting for consistent order
            error_log_file.write(error)







if __name__ == "__main__":
    project_name = "ProjectName"
    code_output_folder = "code_output"
    create_dotnet_project(project_name)
    copy_code(project_name, code_output_folder)
    clean_project(project_name)
    missing_packages = find_missing_packages(project_name)
    install_packages(project_name, missing_packages)
    compile_and_log_errors(project_name)
   
    
    