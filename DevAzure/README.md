# DevAzure

A TypeScript CLI for listing Azure DevOps Git branches and pull requests, and
creating pull requests. The application use cases depend on an
`AzureDevOpsGit` port, keeping the Azure DevOps SDK and CLI presentation at the
edges so another frontend can reuse the operations.

## Setup

```sh
npm install
npm run build
```

Provide Azure DevOps connection settings as command options or environment
variables:

| Option | Environment variable |
| --- | --- |
| `--organization <url>` | `AZURE_DEVOPS_ORGANIZATION` |
| `--project <name>` | `AZURE_DEVOPS_PROJECT` |
| `--repository <name>` | `AZURE_DEVOPS_REPOSITORY` |
| `--token <pat>` | `AZURE_DEVOPS_PAT` |

Use a PAT with the minimum required Git permissions. Avoid putting the token
directly in shell history; prefer the environment variable.

## Commands

```sh
npm start -- branches
npm start -- pull-requests
npm start -- create-pull-request \
  --title "Add feature" \
  --source feature/my-change \
  --target main \
  --description "Summary of the change"
```

Each command returns JSON on standard output. Connection or API failures are
reported on standard error and result in a nonzero exit code.
