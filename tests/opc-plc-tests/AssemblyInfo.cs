using NUnit.Framework;

// As client-side tests are passive and mostly sleep, we can use run many tests in parallel
// regardless of the number of cores.
[assembly: LevelOfParallelism(16)]