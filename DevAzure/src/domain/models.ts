export interface Branch {
  name: string;
  objectId?: string;
}

export interface PullRequest {
  id: number;
  title: string;
  description?: string;
  status?: string;
  sourceBranch: string;
  targetBranch: string;
  url?: string;
  createdBy?: string;
}

export interface CreatePullRequestInput {
  title: string;
  description?: string;
  sourceBranch: string;
  targetBranch: string;
  isDraft?: boolean;
}
