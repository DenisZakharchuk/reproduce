import * as azdev from "azure-devops-node-api";
import { GitApi } from "azure-devops-node-api/GitApi";
import * as GitInterfaces from "azure-devops-node-api/interfaces/GitInterfaces";
import {
  Branch,
  CreatePullRequestInput,
  PullRequest,
} from "../domain/models";
import { AzureDevOpsGit } from "../application/ports/azure-devops-git";

export interface AzureDevOpsConnectionOptions {
  organizationUrl: string;
  project: string;
  repository: string;
  personalAccessToken: string;
}

export class AzureDevOpsGitClient implements AzureDevOpsGit {
  private constructor(
    private readonly gitApi: GitApi,
    private readonly project: string,
    private readonly repository: string,
  ) {}

  static async connect(
    options: AzureDevOpsConnectionOptions,
  ): Promise<AzureDevOpsGitClient> {
    const handler = azdev.getPersonalAccessTokenHandler(
      options.personalAccessToken,
    );
    const connection = new azdev.WebApi(options.organizationUrl, handler);
    const gitApi = await connection.getGitApi();

    return new AzureDevOpsGitClient(
      gitApi,
      options.project,
      options.repository,
    );
  }

  async listBranches(): Promise<Branch[]> {
    const branches = await this.gitApi.getBranches(
      this.repository,
      this.project,
    );

    return branches.map((branch) => ({
      name: requiredApiValue(branch.name, "branch name"),
      objectId: branch.commit?.commitId,
    }));
  }

  async listPullRequests(): Promise<PullRequest[]> {
    const pullRequests = await this.gitApi.getPullRequests(
      this.repository,
      { status: GitInterfaces.PullRequestStatus.All },
      this.project,
    );

    return pullRequests.map((pullRequest) => this.toPullRequest(pullRequest));
  }

  async createPullRequest(
    input: CreatePullRequestInput,
  ): Promise<PullRequest> {
    const request: GitInterfaces.GitPullRequest = {
      title: input.title,
      description: input.description,
      sourceRefName: toBranchRef(input.sourceBranch),
      targetRefName: toBranchRef(input.targetBranch),
      isDraft: input.isDraft,
    };
    const created = await this.gitApi.createPullRequest(
      request,
      this.repository,
      this.project,
    );

    return this.toPullRequest(created);
  }

  private toPullRequest(
    pullRequest: GitInterfaces.GitPullRequest,
  ): PullRequest {
    return {
      id: requiredApiValue(pullRequest.pullRequestId, "pull request ID"),
      title: requiredApiValue(pullRequest.title, "pull request title"),
      description: pullRequest.description,
      status:
        pullRequest.status === undefined
          ? undefined
          : GitInterfaces.PullRequestStatus[pullRequest.status],
      sourceBranch: requiredApiValue(
        pullRequest.sourceRefName,
        "pull request source branch",
      ),
      targetBranch: requiredApiValue(
        pullRequest.targetRefName,
        "pull request target branch",
      ),
      url: pullRequest.url,
      createdBy: pullRequest.createdBy?.displayName,
    };
  }
}

function requiredApiValue<T>(
  value: T | undefined,
  field: string,
): T {
  if (value === undefined) {
    throw new Error(`Azure DevOps response did not include ${field}.`);
  }

  return value;
}

function toBranchRef(branch: string): string {
  return branch.startsWith("refs/heads/") ? branch : `refs/heads/${branch}`;
}
