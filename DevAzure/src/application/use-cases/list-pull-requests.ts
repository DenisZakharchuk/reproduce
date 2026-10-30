import { AzureDevOpsGit } from "../ports/azure-devops-git";

export class ListPullRequests {
  constructor(private readonly git: AzureDevOpsGit) {}

  execute() {
    return this.git.listPullRequests();
  }
}
