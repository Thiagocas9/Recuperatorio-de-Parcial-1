#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using HuntingSimulation;
using UnityEditor;
using UnityEngine;

public sealed class SimulationVerification : MonoBehaviour
{
    readonly HashSet<string> hunterStates = new HashSet<string>();
    readonly HashSet<string> boidStates = new HashSet<string>();
    readonly List<string> results = new List<string>();
    bool failed;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-smokeTest") >= 0)
            new GameObject("Verificacion temporal").AddComponent<SimulationVerification>();
    }
    void OnEnable() => Application.logMessageReceived += OnLog;
    void OnDisable() => Application.logMessageReceived -= OnLog;
    void OnLog(string condition, string trace, LogType type)
    {
        if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)
        { failed = true; results.Add("ERROR: " + condition); }
    }
    void Check(bool value, string label)
    {
        results.Add((value ? "PASS " : "FAIL ") + label);
        if (!value) failed = true;
    }
    IEnumerator Start()
    {
        Time.timeScale = 10;
        var hunter = FindFirstObjectByType<HunterAgent>();
        var boids = FindObjectsByType<BoidAgent>(FindObjectsSortMode.None);
        Check(Application.unityVersion == "6000.3.17f1", "Version exacta");
        Check(boids.Length >= 6, "Minimo de seis Boids");
        float end = Time.time + 180;
        while (Time.time < end && !failed)
        {
            hunterStates.Add(hunter.FSM.Current);
            foreach (var boid in boids)
            {
                boidStates.Add(boid.Behaviour);
                if (!boid.Collected && (!boid.WithinBounds(boid.transform.position) || float.IsNaN(boid.transform.position.x)))
                    Check(false, "Posicion valida dentro de bounds: " + boid.name);
            }
            if (InterestObject.ActiveCount > InterestObject.MaximumActive) Check(false, "Maximo de cinco intereses");
            yield return null;
        }
        Check(hunter.Attacks > 0, "Ataques en simulacion");
        Check(hunter.Gathered > 0, "Recoleccion en simulacion");
        Check(BoidAgent.Respawns > 0, "Reaparicion en simulacion");
        Check(InterestObject.DestroyedCount > 0, "Consumo completo de objetos");
        Check(hunterStates.Contains("Rest"), "Rest observado");
        results.Add("Estados cazador: " + string.Join(", ", hunterStates));
        results.Add("Estados Boid: " + string.Join(", ", boidStates));
        results.Add($"Ataques={hunter.Attacks}; recolecciones={hunter.Gathered}; respawns={BoidAgent.Respawns}; objetos consumidos={InterestObject.DestroyedCount}");
        foreach (var boid in boids) boid.gameObject.SetActive(false);
        hunter.gameObject.SetActive(false);
        foreach (var item in FindObjectsByType<InterestObject>(FindObjectsSortMode.None)) item.gameObject.SetActive(false);
        yield return null;
        RunTransitionChecks();
        Directory.CreateDirectory("Documentation");
        File.WriteAllLines("Documentation/Resultados-pruebas.txt", results);
        Debug.Log(failed ? "SMOKE_TEST_FAILED" : "SMOKE_TEST_OK");
        EditorApplication.Exit(failed ? 1 : 0);
    }
    T Make<T>(string label, Vector3 position) where T : AgentMotor
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = label; go.layer = 8; go.transform.position = position;
        T agent = go.AddComponent<T>(); agent.enabled = false;
        return agent;
    }
    void RunTransitionChecks()
    {
        var prey = Make<BoidAgent>("Prueba presa", new Vector3(4, .6f, 0));
        var hunter = Make<HunterAgent>("Prueba cazador", new Vector3(0, .6f, 0));
        hunter.Detected.Add(prey);
        var machine = new StateMachine(new HuntState(hunter));
        machine.Tick(.1f);
        Check(prey.Health == 1 && machine.Current.Name == "Hunt", "Ranged resta un hit y no abandona Hunt");
        int attacks = hunter.Attacks;
        machine.Tick(.1f);
        Check(hunter.Attacks == attacks, "TBA impide segundo ataque inmediato");
        prey.transform.position = new Vector3(15, .6f, 0);
        hunter.Detected.Clear();
        machine.Tick(.1f);
        Check(hunter.Target == prey && machine.Current.Name == "Hunt", "Conserva presa fuera de vision");
        prey.transform.position = new Vector3(30, .6f, 0);
        float energy = hunter.Energy;
        machine.Tick(.1f);
        Check(machine.Current.Name == "Patrol" && hunter.Energy == energy, "Salida de bounds vuelve a Patrol sin recarga");
        prey.transform.position = new Vector3(4, .6f, 0);
        hunter.Detected.Add(prey);
        machine = new StateMachine(new HuntState(hunter));
        hunter.Energy = 0;
        machine.Tick(.1f);
        Check(machine.Current.Name == "Rest", "Agotamiento en Hunt entra en Rest");
        prey.Damage(1); prey.FSM.Tick(.1f);
        attacks = hunter.Attacks;
        machine.Tick(.1f);
        Check(machine.Current.Name == "Rest" && hunter.Attacks == attacks, "Rest ignora vivos y muertos");
        hunter.Energy = hunter.MaxEnergy;
        machine.Tick(.1f);
        Check(machine.Current.Name == "Patrol", "Rest completo retorna Patrol con una sola transicion");
        machine.Tick(.1f);
        Check(machine.Current.Name == "Gather", "Cadaver local tiene prioridad en Patrol");
        prey.Collect(); machine.Tick(.1f);
        Check(machine.Current.Name == "Patrol", "Gather cancela ante cadaver no disponible");
        prey.FSM.Tick(.1f);
        Check(prey.Behaviour == "Respawn" && prey.Collected, "Dead pasa a Respawn solo tras recoleccion");
        var meleePrey = Make<BoidAgent>("Prueba melee", new Vector3(1.5f, .6f, 8));
        var meleeHunter = Make<HunterAgent>("Prueba melee cazador", new Vector3(0, .6f, 8));
        meleeHunter.Detected.Add(meleePrey);
        var melee = new StateMachine(new HuntState(meleeHunter));
        melee.Tick(.1f); meleePrey.FSM.Tick(.1f);
        Check(meleePrey.Health == 0 && meleePrey.Behaviour == "Dead", "Melee aplica dos hits y Dead tiene prioridad");
        Check(Steering.Force(Vector3.one * 100, Vector3.zero, .4f, 12).magnitude <= 12.001f, "Contribuciones steering limitadas antes de pesos");
        Check(Steering.Arrive(Vector3.zero, Vector3.right, 4, 2, 3) == Vector3.zero, "Arrive frena fuera del volumen del objeto");
    }
}

#endif
