using System;
using System.Xml;
using UnityEngine;

class BaseOsm
{
    protected T GetAttribute<T>(string attrName, XmlAttributeCollection attributes)
    {
        // TODO: We are going to assume 'attrName' exists in the collection
        string strValue = attributes[attrName].Value;
        T instance = default(T);

        try
        {
            instance = (T)Convert.ChangeType(strValue, typeof(T));
        }
        catch (Exception e)
        {
            // Fix for height values ending with 'm' which seems to be something new.
            if (strValue.EndsWith("m"))
            {
                var idx = strValue.IndexOf(' ');
                if (idx >= 0)
                {
                    var tmp = strValue.Substring(0, idx);
                    instance = (T)Convert.ChangeType(tmp, typeof(T));
                }
            }
            else
            {
                Debug.Log(e.ToString() + "\r\nname=" + attrName + ", value=" + strValue);
            }
        }

        return instance;
    }
}
