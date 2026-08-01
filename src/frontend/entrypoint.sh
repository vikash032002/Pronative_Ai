#!/bin/sh
set -eu

TOKEN='__VITE_API_URL__'
FILE='/usr/share/nginx/html/index.html'

if [ ! -f "$FILE" ]; then
  exit 0
fi

VITE_API_URL_VALUE="${VITE_API_URL:-}"

# Escape replacement text for sed.
ESCAPED_REPLACEMENT=$(printf '%s' "$VITE_API_URL_VALUE" | sed -e 's/[&]/\\&/g' -e 's|/|\\/|g')

sed -i "s|$TOKEN|$ESCAPED_REPLACEMENT|g" "$FILE"
