# 🛡️ Sentinel

Sentinel is a modern, local-first Windows security investigation tool built for everyday users, power users, and cybersecurity learners.

It helps identify suspicious downloads, inspect Windows executables, and detect potentially malicious use of built-in Windows tools — all without sending your data anywhere.

Your files stay on your device.

---

## ✨ Features

* Download monitoring and inspection
* Real-time suspicious file detection
* Windows executable (PE) analysis
* EXE, DLL, and driver inspection
* File hashing (MD5, SHA1, SHA256)
* YARA rule support
* PE section and entropy analysis
* Suspicious API import detection
* LOLBin abuse detection
* PowerShell activity inspection
* Risk scoring and severity ratings
* Plain-English security explanations
* Historical scan reporting
* Local SQLite scan database
* JSON, CSV, and HTML report exports
* Light and dark theme support

---

## 🖥️ System Requirements

* Windows 10 or Windows 11
* Python 3.11 or newer
* Visual Studio Code (optional)

---

## 📦 Installation

### Option 1: Run from Source

1. Clone the repository

   git clone https://github.com/Hazza-uxdev/sentinel.git

   cd sentinel

2. Create a virtual environment

   python -m venv .venv

3. Activate the environment

   .venv\Scripts\activate

4. Install dependencies

   pip install -r requirements.txt

5. Run Sentinel

   python main.py

---

### Option 2: Build a Standalone Executable

1. Install PyInstaller

   pip install pyinstaller

2. Build the application

   pyinstaller main.py --onefile --windowed

3. Locate the executable

   dist/

4. Run

   Sentinel.exe

---

## 🚀 First Launch

* Choose your preferred theme
* Configure your Downloads folder location (optional)
* Import YARA rules (optional)
* Enable automatic monitoring

Sentinel will begin monitoring suspicious downloads and activity immediately.

---

## 📁 Data Storage

All data is stored locally on your device.

Application data is stored at:

%APPDATA%/Sentinel/

This may include:

* Scan history database
* Generated reports
* User preferences
* Imported YARA rules
* Application logs

No data is uploaded or synchronized externally.

---

## 🛡️ Security

* Local-first design
* No cloud services
* No telemetry
* No file uploads
* Offline malware triage
* Static analysis only
* Never executes analyzed files
* User-controlled YARA rules
* Fully local report generation

Sentinel is designed to help investigate suspicious activity without introducing additional risk.

---

## 🔍 Threat Analysis

### Download Inspector

Monitors downloaded files and flags:

* Executables
* Scripts
* Double extensions
* Suspicious filenames
* Potential malware indicators

Examples:

* invoice.pdf.exe
* photo.jpg.scr
* document.docx.js

---

### PE File Analyzer

Analyze Windows executables without running them.

Displays:

* File metadata
* PE structure
* Section entropy
* Imported APIs
* Exported functions
* Digital signature status
* Hash information
* YARA matches

---

### Activity Monitor

Detects potentially suspicious use of built-in Windows tools.

Examples:

* powershell.exe
* certutil.exe
* rundll32.exe
* regsvr32.exe
* mshta.exe
* wscript.exe
* cscript.exe

Sentinel explains findings in plain English and provides risk context rather than simply generating alerts.

---

## 🧩 YARA Support

Sentinel supports custom YARA rules.

Add rules to:

data/yara_rules/

Or import them directly through the application.

Loaded rules can be used during file analysis and download inspection.

---

## 🧩 Tech Stack

* Python 3.11+
* CustomTkinter
* psutil
* pefile
* watchdog
* yara-python
* SQLite
* JSON reporting

---

## 🛠 Roadmap

* Digital signature verification improvements
* VirusTotal integration (optional)
* Scheduled scan profiles
* Expanded LOLBin detection rules
* Enhanced YARA management
* Threat intelligence enrichment
* Portable mode
* Plugin support
* Additional forensic reporting

---

## 📄 License

MIT License

---

## ❤️ Credits

Myself of course,

Built for local-first security, transparency, and learning.
