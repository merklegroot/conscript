#!/usr/bin/env bash
set -euo pipefail

# One-time global install for Homebrew Python (PEP 668 requires --break-system-packages).
python3 -m pip install ipykernel -U --user --break-system-packages

echo "ipykernel installed for $(python3 --version)"
echo "Reload the notebook and use the default Python 3 kernel."
