//This code ha been heavily influenced by ChatGPT. Thus, a more robust implementation of the code is being awaited

public class ShapeFileReader : MonoBehaviour
{
    [Tooltip("Path to shapefile (.shp)")]
    public string ShapeFilePath = "\"C:\\Users\\dassu\\Downloads\\LOD1_Building_with_height_info\\LOD1_Building_with_height_info.shp\"";

    private List<Vector3[]> lines;          //Stores lines for visualisation
    private List<Vector3[]> polygons;       //Stores vertices of Polygons
    private List<Vector3> points;           //Stores point features
    void Start()
    {
        lines = new List<Vector3[]>();
        polygons = new List<Vector3[]>();
        points = new List<Vector3>();

        ReadShapefile();

        DrawData();
    }
    void ReadShapefile()
    {
        Debug.Log($"Reading Shapefile from path: {ShapeFilePath}");

        if (!System.IO.File.Exists(ShapeFilePath))
        {
            Debug.LogError($"FIle not found: {ShapeFilePath}");
        }


        using (ShapefileDataReader reader = new(ShapeFilePath, GeometryFactory.Default))
        {
            while (reader.Read())
            {
                Geometry geometry = reader.Geometry; ;

                //For Point or Polygon conversion
                if (geometry is Point point)
                {
                    points.Add(ToUnityCoordinates(point));
                }
                else if (geometry is Polygon polygon)
                {

                    polygons.Add(ConvertToUnityCoordinates(polygon.ExteriorRing.Coordinates));

                }
                else if (geometry is LineString lineString)
                {
                    lines.Add(ConvertToUnityCoordinates(lineString.Coordinates));
                }
            }
        }
    }

    Vector3 ToUnityCoordinates(Point point)
    {
        return new Vector3((float)point.X, 0, (float)point.Y);
    }

    Vector3[] ConvertToUnityCoordinates(Coordinate[] coordinates)
    {
        Vector3[] unityCoordinates = new Vector3[coordinates.Length];
        for (int i = 0; i < coordinates.Length; i++)
        {
            unityCoordinates[i] = new Vector3((float)coordinates[i].X, 0, (float)coordinates[i].Y);

        }
        return unityCoordinates;
    }

    void DrawData()
    {
        foreach (var point in points)
        {
            Debug.DrawRay(point, Vector3.up * 10, Color.green, 100f);
        }
        foreach (var polygon in polygons)
        {
            for (int i = 0; i < polygon.Length; i++)
            {
                Debug.DrawLine(polygon[i], polygon[i + 1], Color.blue, 100f);
            }
        }
        foreach (var line in lines)
        {
            for (int i = 0; i < line.Length; i++)
            {
                Debug.DrawLine(line[i], line[i + 1], Color.red, 100f);
            }
        }
    }
}
