using System.Collections.Generic;
using System.Xml;
using UnityEngine;

class MapReader : MonoBehaviour
{
    [HideInInspector]
    public Dictionary<ulong, OsmNode> nodes;
    [HideInInspector]
    public List<OsmWay> ways;
    [HideInInspector]
    public OsmBounds bounds;
    
    [Tooltip("The file that contains the OSM map data")]
    public string resourceFile;

    public bool IsReady { get; private set; }
    
    // Start is called before the first frame update
    void Start()
    {
        nodes = new Dictionary<ulong, OsmNode>();
        ways = new List<OsmWay>();

        TextAsset txtAsset = Resources.Load<TextAsset>(resourceFile);
        
        // Todo: Make the loading of file a coroutine
        XmlDocument doc = new XmlDocument();
        doc.LoadXml(txtAsset.text);

        // Parse XML file to extract map bounds, nodes and ways
        SetBounds(doc.SelectSingleNode("/osm/bounds"));
        GetNodes(doc.SelectNodes("/osm/node"));
        GetWays(doc.SelectNodes("/osm/way"));

        IsReady = true;
    }

    void Update()
    {
        foreach (OsmWay w in ways)
        {
            if (w.Visible)
            {
                Color c = Color.cyan; // Cyan for buildings
                if (!w.IsBoundary) c = Color.red; // Red for roads

                if (w.NodeIDs.Count > 0)
                {
                    OsmNode p1 = nodes[w.NodeIDs[0]];
                    for (int i = 1; i < w.NodeIDs.Count; i++)
                    {
                        OsmNode p2 = nodes[w.NodeIDs[i]];
                        Vector3 v1 = p1 - bounds.Centre;
                        Vector3 v2 = p2 - bounds.Centre;
                        Debug.DrawLine(v2, v1, c);

                        p1 = p2;
                    }
                }
            }
        }
    }

    void GetWays(XmlNodeList xmlNodeList)
    {
        foreach (XmlNode n in xmlNodeList)
        {
            OsmWay way = new(n);
            ways.Add(way);
        }
    }

    void GetNodes(XmlNodeList xmlNodeList)
    {
        foreach (XmlNode n in xmlNodeList)
        {
            OsmNode node = new(n);
            nodes[node.ID] = node;
        }
    }

    void SetBounds(XmlNode xmlNode)
    {
        bounds = new(xmlNode);
    }
}
