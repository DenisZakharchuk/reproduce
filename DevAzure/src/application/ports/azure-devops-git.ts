import {
  Branch,
  CreatePullRequestInput,
  PullRequest,
} from "../../domain/models";

export interface AzureDevOpsGit {
  listBranches(): Promise<Branch[]>;
  listPullRequests(): Promise<PullRequest[]>;
  createPullRequest(input: CreatePullRequestInput): Promise<PullRequest>;
}
