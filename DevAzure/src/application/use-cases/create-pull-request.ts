import { CreatePullRequestInput } from "../../domain/models";
import { AzureDevOpsGit } from "../ports/azure-devops-git";

export class CreatePullRequest {
  constructor(private readonly git: AzureDevOpsGit) {}

  execute(input: CreatePullRequestInput) {
    return this.git.createPullRequest(input);
  }
}
