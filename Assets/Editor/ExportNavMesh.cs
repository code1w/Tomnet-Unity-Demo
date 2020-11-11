using System.IO;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.AI;


//navmesh导出数据
public class NavMeshExport : MonoBehaviour
{
    [MenuItem("NavMesh/Export")]
    static void Export()
    {
        Debug.Log("NavMesh Export Start");

        NavMeshTriangulation navMeshTriangulation = NavMesh.CalculateTriangulation();

        //文件路径 路径文件夹不存在会报错 
        string path = Application.dataPath + "/AStar/obj/" + SceneManager.GetActiveScene().name + ".obj";

        //新建文件
        StreamWriter streamWriter = new StreamWriter(path);

        //顶点  
        for (int i = 0; i < navMeshTriangulation.vertices.Length; i++)
        {
            streamWriter.WriteLine("v  " + navMeshTriangulation.vertices[i].x + " " + navMeshTriangulation.vertices[i].y + " " + navMeshTriangulation.vertices[i].z);
        }

        streamWriter.WriteLine("g pPlane1");

        //索引  
        for (int i = 0; i < navMeshTriangulation.indices.Length;)
        {
            streamWriter.WriteLine("f " + (navMeshTriangulation.indices[i] + 1) + " " + (navMeshTriangulation.indices[i + 1] + 1) + " " + (navMeshTriangulation.indices[i + 2] + 1));
            i = i + 3;
        }

        streamWriter.Flush();
        streamWriter.Close();


        AssetDatabase.Refresh();

        Debug.Log("NavMesh Export Success");
    }
}