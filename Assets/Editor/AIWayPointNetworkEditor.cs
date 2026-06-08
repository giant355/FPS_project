using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
//--------------------------------------------------------------------
//һ��Edtior�ű���Ӧһ�����
//--------------------------------------------------------------------
[CustomEditor(typeof(AIWayPointNetwork))]
public class AIWayPointNetworkEditor : Editor
{
    void OnSceneGUI()
    {
        AIWayPointNetwork network = (AIWayPointNetwork)target;
        for (int i = 0; i < network.Waypoints.Count; i++)
        {
            if(network.Waypoints[i]!=null)
                Handles.Label(network.Waypoints[i].position,"Waypoint"+i);
        }
        Vector3[] linePoints = new Vector3[network.Waypoints.Count+1];

        if(network.DisplayMode==PathDisplayMode.Connections)
        {
            for(int i = 0;i < network.Waypoints.Count+1;i++)
            {
                int index = (i != network.Waypoints.Count) ? i : 0;
                if(network.Waypoints[index]!=null)
                {
                    linePoints[i] = network.Waypoints[index].position;
                }
            }
            Handles.color = Color.cyan;
            Handles.DrawPolyLine(linePoints);
        }
        else if(network.DisplayMode==PathDisplayMode.Paths)
        {
            NavMeshPath path=new NavMeshPath();
            Vector3 from = network.Waypoints[network.UIStart].position;
            Vector3 to = network.Waypoints[network.UIEnd].position;

            NavMesh.CalculatePath(from, to,NavMesh.AllAreas ,path);

            Handles.color= Color.yellow;
            Handles.DrawPolyLine(path.corners);
        }
    }
    public override void OnInspectorGUI()
    {
        AIWayPointNetwork network = (AIWayPointNetwork)target;

        network.DisplayMode=(PathDisplayMode)EditorGUILayout.EnumPopup("Display Mode",network.DisplayMode);
        if (network.DisplayMode == PathDisplayMode.Paths)
        { 
            network.UIStart=EditorGUILayout.IntSlider("Waypoint Start",network.UIStart,0,network.Waypoints.Count-1);
            network.UIEnd=EditorGUILayout.IntSlider("Waypoint End",network.UIEnd,0, network.Waypoints.Count - 1);
        }

        //base.OnInspectorGUI();
        DrawDefaultInspector();

    }

}
