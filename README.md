## Actual Scenarios:

1.(DONE) When a leader is active it sends a heart beat within 50ms.  

2.(DONE) When a node receives an AppendEntries from another node, then first node remembers that other node is the current leader.  

3.(DONE) When a new node is initialized, it should be in follower state.

4.(DONE) When a follower doesn't get a message for 300ms then it starts an election.  

5.(DONE) When the election time is reset, it is a random value between 150 and 300ms.  
   - Between  
   - Random: call n times and make sure that there are some that are different (otherproperties of the distribution if you like) 

6.(DONE)When a new election begins, the term is incremented by 1.  
   - Create a new node, store id in variable.  
   - Wait 300 ms  
   - Reread term (?)  
   - Assert after is greater (by at least 1)  

7.(DONE) When a follower does get an AppendEntries message, it resets the election timer. (i.e. it doesn't start an election even after more than 300ms)  

8.Given an election begins, when the candidate gets a majority of votes, it becomes a leader. (think of the easy case; can use two tests for single and multi-node clusters)  

9.Given a candidate receives a majority of votes while waiting for unresponsive node, it still becomes a leader.  

10.A follower that has not voted and is in an earlier term responds to a RequestForVoteRPC with yes. (the reply will be a separate RPC) 

11.Given a candidate server that just became a candidate, it votes for itself.  

12.Given a candidate, when it receives an AppendEntries message from a node with a later term, then candidate loses and becomes a follower. 

13.Given a candidate, when it receives an AppendEntries message from a node with an equal term, then candidate loses and becomes a follower.

14.If a node receives a second request for vote for the same term, it should respond no. (again, separate RPC for response)  

15.If a node receives a second request for vote for a future term, it should vote for that node.  

16.Given a candidate, when an election timer expires inside of an election, a new election is started.  

17.(DONE)  When a follower node receives an AppendEntries request, it sends a response.  

18.(DONE) Given a candidate receives an AppendEntries from a previous term, then rejects.  

19.When a candidate wins an election, it immediately sends a heart beat.  

20.(Testing persistence to disk will be a later assignment.)  

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

- Ensure each node has a unique identifier to prevent recording two votes from the same node.  
- When using the `RequestVoteRPC` to request a vote from another node, they will call the my `VoteResponseRPC` method.  
- If a vote request fails, it might retry and call the `VoteResponseRPC` method again. However, be cautious since a node can only vote once.  
- Heartbeats must occur more frequently (every 20ms) than the election timeout.  
- For heartbeats, iterate over every node in the neighbor list and call its `RequestAppendEntries` method with no arguments.  
- Keep track of the leader by storing the leader's unique ID.  
- In step 7, send an RPC every 100ms and ensure that no election starts after 300ms.  
- Ensure all RPC methods are asynchronous.  