export interface VoteDistribution {
  groupId: number
  count: number
}

export interface VoteResponse {
  voteCount: number
  myVoteCount: number
}

export interface VoteCountChangedPayload {
  groupId: number
  newCount: number
}
