import { AzureDevOpsGit } from "../ports/azure-devops-git";

export class ListBranches {
  constructor(private readonly git: AzureDevOpsGit) {}

  execute() {
    return this.git.listBranches();
  }
}
