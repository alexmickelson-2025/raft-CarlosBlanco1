## Scenarios:

1. Given a cluster with 3 uninitilaized servers 
When the servers are initialized and start running
Then they should all be in follower state by default

2. Given a cluster with 3 follower servers
When the election timeout has passed
Then one of the servers should increment its current term and transition to candidate state

3. Given a cluster with 3 servers, 2 followers and 1 candidate
When the candidate is initialized and votes for itself
Then it should send RequestVoteRPCs to the follower servers

4. Given a cluster with 3 servers, 2 followers and 1 candidate
When the candidate has received votes from the 2 follower servers
Then it should transition to leader state

5. Given a cluster with 3 servers, 2 followers and 1 leader
When the leader is initialized
Then it should send empty AppendEntriesRPCs (heartbeats) to the follower servers

6. Given a cluster with 3 servers, 1 follower, 1 candidate and 1 leader
When the candidate server receives a AppendEntriesRPC from the leader with the same term
Then it should transition back to the follower state

7. Given a cluster with 3 servers, 1 follower, 1 candidate and 1 leader
When the candidate server receives a AppendEntriesRPC from the leader with a higher term
Then it should transition back to the follower state

8. Given a cluster with 3 servers, 1 follower, 1 candidate and 1 leader
When the candidate server receives a AppendEntriesRPC from the leader with a lower term
Then it should stay in the candidate state

9. Given a cluster with 4 servers, 2 followers and 2 candidates with the same randomized timoeouts
When both candidates fail to receive a majority of votes
Then both should restart their election process with new randomized timeouts

10. Given a cluster with 6 servers, 4 followers and 2 candidates with the same randomized timeouts
When both candidates fail to receive a majority of votes
Then both should restart their election process with new randomized timeouts.

11. Given a cluster with 4 servers, 2 followers and 2 candidates with different randomized timeouts
When both candidates send their RequestVoteRPCs
Then the candidate with the lower randomized timeout should get the majority of the votes and be elected leader

12. Given a cluster with 2 servers, both of them in candidate state, with different terms
When one receives a RequestVoteRPC from the other candidate with a higher term
Then it should transition back to follower state

13. Given a cluster with 1 uninitilaized servers
When the server is initialized and starts running
Then it should be in follower state by default

14. Given a cluster with 4 follower servers
When the election timeout has passed
Then at least one of the servers should increment its current term and transition to candidate state.

15. Given a cluster with 2 uninitialized servers
When the servers are initialized and start running
Then they should both be in follower state by default.

16. Given a cluster with 6 servers, 5 followers and 1 candidate
When the one candidate receives the votes
Then it should transition to leader state and immediately send AppendEntriesRPCs (heartbeats) to all follower servers.

17. Given a cluster with 5 servers, 3 followers and 2 candidates with different randomized timeouts
When the one candidate receives votes from the three followers
Then it should transition to leader state, and the other candidate should remain in the candidate state.

## Considerations:

1. All data structures, log entries, current term, and vote counts should be protected against concurrent access.
2. Handling both RequestVoteRPCs and AppendEntriesRPCs concurrently while making it thread safe.
3. Making sure AppendEntriesRPCs are sent concurrently from the leader server to every other one.
4. Making sure the randomized timeouts are selected within a given range (pdf says between 150-300 ms)
5. Making sure hearbeat frequency is consistent.
6. Make sure the randomized election timeout is appropiate and greater than the heartbeat frequency.
7. Make sure transition periods from one role to another don't ever allow for 2 leaders at the same time.
8. If we're doing logs, making sure their indeces are consistent.
9. Making sure term updates occur when they're supposed to.
10. In case of tie in an election, making sure the new election cycle starts appropiately and not allowing for infinite split votes.