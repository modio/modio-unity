// using System;
// using System.Threading.Tasks;
// using UnityEngine;
//
// namespace ModIO.Implementation
// {
//     class TaskQueueRunnerResult : TaskPriorityQueueRunner<object>
//     {
//         //I don't know how to approach this any more
//
//         public TaskQueueRunnerResult() : base(1, true) { }
//
//         //Okay, so I think this doesn't work, due to the cast... hmmm
//         //I think the problem is that we start the task, return the result, before it's been done running
//         //Via Task.Factory.StartNew()
//         //TECHNICALLY it should await the result?
//
//         //This means we can make it generic way easier! we just have to specify that its a non-class
//         //and another with a class as T! Sweet. And Generic!
//         //Assuming it works
//         public async Task<Result> AddTaskResult2(TaskPriority priority, int taskSize, Task<Result> task)
//         {
//             Debug.Log("TaskResult2");
//             Func<Task<object>> castWrapper = async () =>
//             {
//                 Debug.Log("castWrapper");
//                 Result res = await task;
//                 return res as object;
//             };
//             Task<object> t = AddTask(priority, taskSize, new Task<object>(castWrapper));
//             var tOutput = await t; //So, it fails because it doesn't await? but i am awaiting and running?
//                                    //Result is a task?
//
//             //okay, this task is not viable
//             //okay, this is progress, but nothing seems to be running this task?
//
//             //So, I think i have to run the conversion task
//             //but thats not working?
//             //Honestly this is too complex we need to go back to the drawing board
//             //we may actually need to merge result and resultand somehow
//             //interface should work
//             
//             Task<object> conversionTask = ((Task<object>)t.Result);
//             var task3wtf = Task.Run(() => Task.FromResult(conversionTask));
//             await task3wtf;
//             //conversionTask.RunSynchronously();
//             //conversionTask.RunSynchronously();
//
//             //Why doesn't it run this?
//             //object conversionOutput = await AddTask(priority, 1, conversionTask);
//             //Task.Run(conversionTask);
//
//             Debug.Log($"Returning value in taskresult2 {t} completed:{t.IsCompleted} result:{t.Result}");
//             Debug.Log($"Returning value in conversionOutput completed:{conversionTask.IsCompleted} result:{conversionTask.Result}");
//             return (Result)t.Result;
//         }
//
//         public async Task<Result> AddTaskResult(TaskPriority priority, int taskSize, Task<Result> inputTask)
//         {
//             Func<Task<object>> func = async () =>
//             {
//                 //This crashes?
//                 Result result = await inputTask; //okay, what is input task?
//
//                 return Task.Factory.StartNew(() => result as object);                
//             };
//
//             Task<object> task = AddTask(priority, taskSize, func);
//             Task<Result> resultWrapper = Task.Factory.StartNew(() => (Result)task.Result); //this can't be right
//             return await resultWrapper;
//         }
//
//         public async Task<ResultAnd<T>> AddTaskResultAnd<T>(TaskPriority priority, int taskSize, Task<ResultAnd<T>> inputTask)
//         {
//             Func<Task<object>> func = async () =>
//             {
//                 ResultAnd<T> result = await inputTask;
//                 return Task.Factory.StartNew(() => result as object);
//             };
//
//             Task<object> task = AddTask(priority, taskSize, func);
//             Task<ResultAnd<T>> resultWrapper = Task.Factory.StartNew(() => task.Result as ResultAnd<T>);
//             return await resultWrapper;
//         }
//     }
// }
