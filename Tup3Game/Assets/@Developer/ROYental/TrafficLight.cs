using System;
using CleverCrow.Fluid.BTs.Tasks;
using CleverCrow.Fluid.BTs.Trees;
using UnityEngine;

public class TrafficLight : MonoBehaviour
{
    [SerializeField] BehaviorTree behaviorTree;

    private float timer = 0;
    private void Awake()
    {
        behaviorTree = new BehaviorTreeBuilder(gameObject)
            .Sequence("Root")
            .Do("Red", Red).Do("Green", Green)
            .End().Build();
    }

    private void Update()
    {
        behaviorTree.Tick();
    }

    private TaskStatus Red()
    {
        Debug.Log("Red");
        timer += Time.deltaTime;
        if (timer > 10)
        {
            timer = 0;
            return TaskStatus.Success;
        }
        return TaskStatus.Continue;
    }

    private TaskStatus Green()
    {
        Debug.Log("Green");
        timer += Time.deltaTime;
        if (timer > 5)
        {
            timer = 0;
            return TaskStatus.Success;
        }
        return TaskStatus.Continue;
    }
}
