import { Command, Option, OptionValues } from "commander";
import { CreatePullRequest } from "../application/use-cases/create-pull-request";
import { ListBranches } from "../application/use-cases/list-branches";
import { ListPullRequests } from "../application/use-cases/list-pull-requests";
import {
  AzureDevOpsConnectionOptions,
  AzureDevOpsGitClient,
} from "../infrastructure/azure-devops-git-client";

interface CliOptions extends OptionValues {
  organization?: string;
  project?: string;
  repository?: string;
  token?: string;
}

export function createCli(): Command {
  const program = new Command();
  program
    .name("devazure")
    .description("List Azure DevOps branches and pull requests, or create a PR.")
    .addOption(
      new Option("--organization <url>", "Azure DevOps organization URL").env(
        "AZURE_DEVOPS_ORGANIZATION",
      ),
    )
    .addOption(
      new Option("--project <name>", "Azure DevOps project").env(
        "AZURE_DEVOPS_PROJECT",
      ),
    )
    .addOption(
      new Option("--repository <name>", "Azure DevOps Git repository").env(
        "AZURE_DEVOPS_REPOSITORY",
      ),
    )
    .addOption(
      new Option("--token <pat>", "Azure DevOps personal access token").env(
        "AZURE_DEVOPS_PAT",
      ),
    );

  program
    .command("branches")
    .description("List repository branches")
    .action(async () => {
      const git = await connect(program.opts<CliOptions>());
      const branches = await new ListBranches(git).execute();
      printJson(branches);
    });

  program
    .command("pull-requests")
    .description("List repository pull requests")
    .action(async () => {
      const git = await connect(program.opts<CliOptions>());
      const pullRequests = await new ListPullRequests(git).execute();
      printJson(pullRequests);
    });

  program
    .command("create-pull-request")
    .description("Create a pull request")
    .requiredOption("--title <title>", "Pull request title")
    .requiredOption("--source <branch>", "Source branch")
    .requiredOption("--target <branch>", "Target branch")
    .option("--description <text>", "Pull request description")
    .option("--draft", "Create as a draft pull request")
    .action(async (options: {
      title: string;
      source: string;
      target: string;
      description?: string;
      draft?: boolean;
    }) => {
      const git = await connect(program.opts<CliOptions>());
      const pullRequest = await new CreatePullRequest(git).execute({
        title: options.title,
        sourceBranch: options.source,
        targetBranch: options.target,
        description: options.description,
        isDraft: options.draft,
      });
      printJson(pullRequest);
    });

  return program;
}

async function connect(
  options: CliOptions,
): Promise<AzureDevOpsGitClient> {
  const connectionOptions: AzureDevOpsConnectionOptions = {
    organizationUrl: requiredOption(
      options.organization,
      "organization",
      "AZURE_DEVOPS_ORGANIZATION",
    ),
    project: requiredOption(options.project, "project", "AZURE_DEVOPS_PROJECT"),
    repository: requiredOption(
      options.repository,
      "repository",
      "AZURE_DEVOPS_REPOSITORY",
    ),
    personalAccessToken: requiredOption(
      options.token,
      "token",
      "AZURE_DEVOPS_PAT",
    ),
  };

  return AzureDevOpsGitClient.connect(connectionOptions);
}

function requiredOption(
  value: string | undefined,
  option: string,
  environmentVariable: string,
): string {
  if (!value) {
    throw new Error(
      `Missing --${option}; provide it or set ${environmentVariable}.`,
    );
  }

  return value;
}

function printJson(value: unknown): void {
  console.log(JSON.stringify(value, null, 2));
}
