# actor_concurrency

A sample app using Proto.Actor which show cases concurrency.

The main library used is [Proto.Actor](https://proto.actor).
It's .Net [actor](https://en.wikipedia.org/wiki/Actor_model) 
framework with a lot of functionaltiy.

## Commands

The following commands can be given to the program in order to
execute a test.

* sort - Just sorts the test data using single thread.
* actor_sort - Sort using actors, one iteration at a time.
* actor_sort_async - Sort using actors, spawn all iterations at once.
* actor_sort_split - Sort using actors, split the range in smaller pieces.
* actor_sort_async_split - Sort using actors, combine split and async.
* char_pairs - Find the maximum number of unique chars in word pairs formed from the test data.
* actor_char_pairs - Same as 'char_pais' with actors.
* actor_char_pairs_pooled - Same as 'char_pais' with actors using a worker pool.
* task_char_pairs - Same as 'char_pais' with C# Task system (no actors).