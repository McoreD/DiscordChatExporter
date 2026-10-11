#!/usr/bin/env bash
set -euo pipefail
umask 077

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
repo_dir="$(cd -- "$script_dir/.." && pwd)"
guild_id=844007820445351947

if [[ ${1:-} == --help ]]; then
  printf '%s\n' \
    'Usage: export-itspersonal.sh [token-file]' \
    'Reads DISCORD_TOKEN, a supplied token file, or DISCORD_TOKEN_FILE.' \
    'In an interactive terminal, prompts for the token if none is available.' \
    'Exports all available history as JSON, including all threads and media.' \
    'DCE_CLI: optional path to an existing DiscordChatExporter.Cli executable.' \
    'DCE_EXPORT_ROOT: optional output directory; defaults to ../ss/DiscordChatExporter/exports/ItsPersonal beside the repository.'
  exit 0
fi

if (( $# > 1 )); then
  printf '%s\n' 'Provide at most one token-file path.' >&2
  exit 2
fi

token_file="${1:-${DISCORD_TOKEN_FILE:-}}"
if [[ -n $token_file ]]; then
  if [[ ! -r $token_file ]]; then
    printf '%s\n' 'The supplied token file is not readable.' >&2
    exit 2
  fi
  DISCORD_TOKEN=''
  IFS= read -r DISCORD_TOKEN < "$token_file" || true
  DISCORD_TOKEN="${DISCORD_TOKEN%$'\r'}"
fi
if [[ -z ${DISCORD_TOKEN:-} && -t 0 ]]; then
  read -r -s -p 'Discord token: ' DISCORD_TOKEN
  printf '\n' >&2
fi
if [[ -z ${DISCORD_TOKEN:-} ]]; then
  printf '%s\n' 'A Discord token is required. Set DISCORD_TOKEN or provide a file containing only the token.' >&2
  exit 2
fi
export DISCORD_TOKEN

if [[ -n ${DCE_CLI:-} ]]; then
  if [[ ! -x $DCE_CLI ]]; then
    printf '%s\n' 'DCE_CLI must point to an executable file.' >&2
    exit 2
  fi
  exporter=("$DCE_CLI")
else
  if ! command -v dotnet > /dev/null; then
    printf '%s\n' 'Install the .NET SDK required by global.json, or set DCE_CLI to an existing executable.' >&2
    exit 2
  fi
  exporter=(dotnet run --project "$repo_dir/DiscordChatExporter.Cli" --configuration Release --)
fi

output_base="${DCE_EXPORT_ROOT:-$repo_dir/../ss/DiscordChatExporter/exports/ItsPersonal}"
mkdir -p -- "$output_base"
export_dir="$(mktemp -d "$output_base/$(date -u +%Y%m%dT%H%M%SZ)-XXXXXX")"
printf 'Export destination: %s\n' "$export_dir"
"${exporter[@]}" exportguild \
  --guild "$guild_id" \
  --format Json \
  --include-threads All \
  --include-vc true \
  --media \
  --reuse-media \
  --parallel 1 \
  --output "$export_dir/" 2>&1 | tee "$export_dir/export.log"
