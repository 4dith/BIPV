using System;
using System.Collections.Generic;
using System.Xml;

class OsmWay : BaseOsm
{
    public ulong ID { get; private set; }
    public bool Visible { get; private set; }
    public List<ulong> NodeIDs { get; private set; }
    public bool IsBoundary { get; private set; }
    public bool IsBuilding { get; private set; }
    public float Height { get; private set; }
    
    public OsmWay(XmlNode node) 
    {
        NodeIDs = new List<ulong>();
        Height = 3.0f;

        ID = GetAttribute<ulong>("id", node.Attributes);
        try
        {
            Visible = GetAttribute<bool>("visible", node.Attributes);
        }
        catch (Exception)
        {
            Visible = true;
        }
        

        XmlNodeList nds = node.SelectNodes("nd");
        foreach (XmlNode n in nds)
        {
            ulong refNo = GetAttribute<ulong>("ref", n.Attributes);
            NodeIDs.Add(refNo);
        }

        if (NodeIDs.Count > 1)
        {
            IsBoundary = NodeIDs[0] == NodeIDs[NodeIDs.Count - 1];
        }

        // For buildings only
        XmlNodeList tags = node.SelectNodes("tag");
        foreach (XmlNode t in tags)
        {
            string key = GetAttribute<string>("k", t.Attributes);
            if (key == "building:levels" && Height == 0)
            {
                Height = 3.0f * GetAttribute<float>("v", t.Attributes);
            } else if (key == "height")
            {
                // Assuming height is in metres
                Height = GetAttribute<float>("v", t.Attributes);
            } else if (key == "building")
            {
                IsBuilding = GetAttribute<string>("v", t.Attributes) == "yes";
            }
        }
    }
}
