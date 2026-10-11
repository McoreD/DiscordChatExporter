# Export ItsPersonal to JSON

This fork includes a Bash workflow for ItsPersonal (server ID `844007820445351947`).
It exports all available history as one JSON file per channel or thread, includes
active and archived threads and text chat in voice channels, and downloads
referenced media alongside the JSON files. No date or message filter is applied.

## Run the export

From the repository root, run:

```bash
./scripts/export-itspersonal.sh
```

The script prompts for a token without displaying it. It also accepts the
`DISCORD_TOKEN` environment variable or a path to a file containing only the
token on its first line:

```bash
./scripts/export-itspersonal.sh /path/outside/the/repository/token.txt
```

The `DISCORD_TOKEN_FILE` environment variable can supply that path instead.
A supplied token file takes precedence over `DISCORD_TOKEN`. Keep tokens outside
the repository and restrict access to the file, for example with `chmod 600`.
For token setup and bot permissions, see [Token and IDs](Token-and-IDs.md).

By default, the workflow runs the CLI from this checkout with `dotnet run`, using
the SDK specified in `global.json`. To use an existing CLI executable, set
`DCE_CLI` to its path:

```bash
DCE_CLI=/path/to/DiscordChatExporter.Cli ./scripts/export-itspersonal.sh
```

## Output and coverage

Each invocation creates a new directory under
`../ss/DiscordChatExporter/exports/ItsPersonal/`, relative to the repository root.
It contains the JSON files, downloaded media, and `export.log`. Set
`DCE_EXPORT_ROOT` to change the parent output directory. Newly created export
directories and files are private to the current user.

The export is limited to channels, threads, messages, and media the authenticated
account can access. Deleted content cannot be recovered. A bot needs the Message
Content Intent enabled and appropriate permissions for all required channels and
private threads. Review `export.log` for channel errors and missing media before
calling an export complete: the CLI can return success when only some channels
were exported successfully. This workflow preserves the CLI's exit status.
