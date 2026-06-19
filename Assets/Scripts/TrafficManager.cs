using UnityEngine;
using System.Collections.Generic;

public class TrafficManager : MonoBehaviour
{
    void Start()
    {
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();

        // Waypoints for Group 1 (Z~156.5)
        Vector3[] waypointsG1 = new Vector3[]
        {
            new Vector3(237f, 0f, 156.5f),
            new Vector3(236.95f, 0f, 123.5f),
            new Vector3(3.5f, 0f, 123.5f),
            new Vector3(3.5f, 0f, 156.5f)
        };

        // Waypoints for Group 2 (Z~123.5)
        Vector3[] waypointsG2 = new Vector3[]
        {
            new Vector3(3.5f, 0f, 123.5f),
            new Vector3(3.5f, 0f, 156.5f),
            new Vector3(237f, 0f, 156.5f),
            new Vector3(236.95f, 0f, 123.5f)
        };

        // Waypoints for Group 3 (Z~163.5)
        Vector3[] waypointsG3 = new Vector3[]
        {
            new Vector3(-3f, 0f, 163.5f),
            new Vector3(-2.34f, 0f, 116.5f),
            new Vector3(243.5f, 0f, 116.5f),
            new Vector3(243f, 0f, 163.5f)
        };

        // Waypoints for Group 4 (Z~116.5)
        Vector3[] waypointsG4 = new Vector3[]
        {
            new Vector3(243.5f, 0f, 116.5f),
            new Vector3(243f, 0f, 163.5f),
            new Vector3(-3f, 0f, 163.5f),
            new Vector3(-2.34f, 0f, 116.5f)
        };

        // Waypoints for Group 5 (Z~56.5)
        Vector3[] waypointsG5 = new Vector3[]
        {
            new Vector3(236.5f, 0f, 56.5f),
            new Vector3(236.5f, 0f, 3f),
            new Vector3(3.3f, 0f, 3f),
            new Vector3(3.5f, 0f, 56.5f)
        };

        // Waypoints for Group 6 (Z~3.0)
        Vector3[] waypointsG6 = new Vector3[]
        {
            new Vector3(3.3f, 0f, 3f),
            new Vector3(3.5f, 0f, 56.5f),
            new Vector3(236.5f, 0f, 56.5f),
            new Vector3(236.5f, 0f, 3f)
        };

        // Waypoints for Group 7 (Z~63.5)
        Vector3[] waypointsG7 = new Vector3[]
        {
            new Vector3(-3.3f, 0f, 63.5f),
            new Vector3(-3.3f, 0f, -3.2f),
            new Vector3(243f, 0f, -3.2f),
            new Vector3(243f, 0f, 63.5f)
        };

        // Waypoints for Group 8 (Z~-3.2)
        Vector3[] waypointsG8 = new Vector3[]
        {
            new Vector3(243f, 0f, -3.2f),
            new Vector3(243f, 0f, 63.5f),
            new Vector3(-3.3f, 0f, 63.5f),
            new Vector3(-3.3f, 0f, -3.2f)
        };

        foreach (var go in allObjects)
        {
            if (go.name.StartsWith("Vehicle_"))
            {

                // Skip active player taxi
                if (go.name == "Vehicle_Taxi (9)")
                {
                    continue;
                }

                float z = go.transform.position.z;

                // Group 1: Z around 156.5 (4 vehicles)
                if (Mathf.Abs(z - 156.5f) < 2.0f)
                {
                    LoopingVehicleAI ai = go.AddComponent<LoopingVehicleAI>();
                    ai.waypoints = waypointsG1;
                    Debug.Log($"[Traffic] Attached Group 1 AI to: {go.name} at {go.transform.position}");
                }
                // Group 2: Z around 123.5 (5 vehicles)
                else if (Mathf.Abs(z - 123.5f) < 2.0f)
                {
                    LoopingVehicleAI ai = go.AddComponent<LoopingVehicleAI>();
                    ai.waypoints = waypointsG2;
                    Debug.Log($"[Traffic] Attached Group 2 AI to: {go.name} at {go.transform.position}");
                }
                // Group 3: Z around 163.5 (4 vehicles)
                else if (Mathf.Abs(z - 163.5f) < 2.0f)
                {
                    LoopingVehicleAI ai = go.AddComponent<LoopingVehicleAI>();
                    ai.waypoints = waypointsG3;
                    Debug.Log($"[Traffic] Attached Group 3 AI to: {go.name} at {go.transform.position}");
                }
                // Group 4: Z around 116.5 (6 vehicles)
                else if (Mathf.Abs(z - 116.5f) < 2.0f)
                {
                    LoopingVehicleAI ai = go.AddComponent<LoopingVehicleAI>();
                    ai.waypoints = waypointsG4;
                    Debug.Log($"[Traffic] Attached Group 4 AI to: {go.name} at {go.transform.position}");
                }
                // Group 5: Z around 56.5 (5 vehicles)
                else if (Mathf.Abs(z - 56.5f) < 5.0f)
                {
                    LoopingVehicleAI ai = go.AddComponent<LoopingVehicleAI>();
                    ai.waypoints = waypointsG5;
                    Debug.Log($"[Traffic] Attached Group 5 AI to: {go.name} at {go.transform.position}");
                }
                // Group 6: Z around 3.0 (6 vehicles)
                else if (Mathf.Abs(z - 3.0f) < 2.0f)
                {
                    LoopingVehicleAI ai = go.AddComponent<LoopingVehicleAI>();
                    ai.waypoints = waypointsG6;
                    Debug.Log($"[Traffic] Attached Group 6 AI to: {go.name} at {go.transform.position}");
                }
                // Group 7: Z around 63.5 (5 vehicles)
                else if (Mathf.Abs(z - 63.5f) < 2.0f)
                {
                    LoopingVehicleAI ai = go.AddComponent<LoopingVehicleAI>();
                    ai.waypoints = waypointsG7;
                    Debug.Log($"[Traffic] Attached Group 7 AI to: {go.name} at {go.transform.position}");
                }
                // Group 8: Z around -3.2 (6 vehicles)
                else if (Mathf.Abs(z - (-3.2f)) < 2.0f)
                {
                    LoopingVehicleAI ai = go.AddComponent<LoopingVehicleAI>();
                    ai.waypoints = waypointsG8;
                    Debug.Log($"[Traffic] Attached Group 8 AI to: {go.name} at {go.transform.position}");
                }
            }
        }
    }
}
