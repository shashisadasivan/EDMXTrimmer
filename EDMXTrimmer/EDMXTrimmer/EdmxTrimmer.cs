using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace EDMXTrimmer
{
    //TODO: add comments on methods and their purpose
    class EdmxTrimmer
    {
        public string EdmxFile { get; private set; }
        private XmlDocument _xmlDocument;
        private const string PREFIXSTR = "-";

        public EdmxTrimmer(string _edmxFile)
        {
            this.EdmxFile = _edmxFile;
        }

        public void Run()
        {
            this.LoadFile();
            this.AnalyzeFile();
        }

        public void Trim(List<String> entitiesToKeep)
        {
            this.LoadFile();
            this.AnalyzeFile(trim:true, entitiesToKeep:entitiesToKeep);
        }

        private void AnalyzeFile(string prefix = "", bool trim = false, List<String> entitiesToKeep = null)
        {
            foreach (XmlNode node in this._xmlDocument.DocumentElement.ChildNodes)
            {
                Console.WriteLine($"{node.Name}");
                this.AnalyzeNode(node, PREFIXSTR, trim, entitiesToKeep);
            }
            if (trim == true)
            {
                this._xmlDocument.Save("Output.edmx");
            }
        }

        private void AnalyzeNode(XmlNode node, string prefix = "", bool trim = false, List<String> entitiesToKeep = null)
        {
            List<XmlNode> nodesToRemove = new List<XmlNode>();

            foreach (XmlNode childNode in node.ChildNodes)
            {
                if (childNode.Name == "EntitySet" || childNode.Name == "EntityType")
                {
                    if (trim == true
                        && entitiesToKeep != null
                        && entitiesToKeep.Count > 0
                        && entitiesToKeep.Contains(childNode.Attributes["Name"].Value, StringComparer.OrdinalIgnoreCase) == false)
                    {
                        // Delete this
                        Console.WriteLine($"Deleting: {prefix}{childNode.Name}-{childNode.Attributes["Name"].Value}");
                        //node.RemoveChild(childNode);
                        nodesToRemove.Add(childNode);
                    }
                    else
                    {
                        //Console.WriteLine($"{prefix}{node.Name}");
                        Console.WriteLine($"{prefix}{childNode.Name}-{childNode.Attributes["Name"].Value}");
                        // Remove NavigationProperties here
                        if (childNode.Name == "EntityType"
                            && childNode.HasChildNodes == true)
                        {
                            foreach (XmlNode childNodeNavProp in childNode.ChildNodes)
                            {
                                if (childNodeNavProp.Name == "NavigationProperty")
                                {
                                    nodesToRemove.Add(childNodeNavProp);
                                }
                            }
                        }
                    }
                }
                else if(trim == true
                        && childNode.Name == "Action")
                {
                    // delete all actions
                    nodesToRemove.Add(childNode);
                }
                else if(childNode.HasChildNodes)
                {
                    this.AnalyzeNode(childNode, prefix + PREFIXSTR, trim, entitiesToKeep);
                }
            }

            if(nodesToRemove.Count > 0)
            {
                foreach (var nodeToRemove in nodesToRemove)
                {
                    XmlNode parentNode = nodeToRemove.ParentNode;
                    parentNode.RemoveChild(nodeToRemove);
                }
            }
        }

        private void LoadFile()
        {
            this._xmlDocument = new XmlDocument();
            this._xmlDocument.Load(this.EdmxFile);
        }
    }
}
