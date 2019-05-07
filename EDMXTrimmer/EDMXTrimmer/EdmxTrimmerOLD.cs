using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace EDMXTrimmer
{
    //TODO: add comments on methods and their purpose
    class EdmxTrimmerOLD
    {
        private const string PREFIXSTR = "-";
        public string EdmxFile { get; private set; }
        private XmlDocument _xmlDocument;
        private bool _verbose;
        
        public List<string> EntitiesToKeep { get; private set; }

        public EdmxTrimmerOLD(string edmxFile, bool verbose = false )
        {
            this.EdmxFile = edmxFile;
            this._verbose = verbose;
        }

        public void Run()
        {
            //this.EntitiesToKeep = new List<string>();
            this.LoadFile();
            this.AnalyzeFile(trim:false, entitiesToKeep:EntitiesToKeep);
        }

        public void Trim(List<String> entitiesToKeep)
        {
            this.LoadFile();
            this.AnalyzeFile(trim:true, entitiesToKeep:entitiesToKeep);
        }

        public void AnalyzeAndTrim(List<String> entitiesToKeep)
        {
            this.EntitiesToKeep = new List<string>();
            this.EntitiesToKeep.AddRange(entitiesToKeep);
            this.Run();
            this.Trim(this.EntitiesToKeep);
        }

        private void AnalyzeFile(string prefix = "", bool trim = false, List<String> entitiesToKeep = null)
        {
            foreach (XmlNode node in this._xmlDocument.DocumentElement.ChildNodes)
            {
                if (this._verbose)
                {
                    Console.WriteLine($"{node.Name}");
                }
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
            if(entitiesToKeep == null)
            {
                entitiesToKeep = new List<string>();
            }
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
                        if (this._verbose)
                        {
                            Console.WriteLine($"Deleting: {prefix}{childNode.Name}-{childNode.Attributes["Name"].Value}");
                        }
                        //node.RemoveChild(childNode);
                        nodesToRemove.Add(childNode);
                    }
                    else
                    {
                        // Remove NavigationProperties here
                        if (trim == true
                            && childNode.Name == "EntityType"
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

                        if (trim == false
                            && childNode.Name == "EntitySet"
                            && entitiesToKeep.Contains(childNode.Attributes["Name"].Value, StringComparer.OrdinalIgnoreCase) == true)
                        {
                            // If this is the entitySet, extract the entityType here
                            var entityType = childNode.Attributes["EntityType"].Value;
                            // this is stored as Microsoft.Dynamics.DataEntities.CustomerV3, we only want the name CustomerV3
                            entityType = entityType.Replace("Microsoft.Dynamics.DataEntities.", "");
                            this.EntitiesToKeep.Add(entityType);
                            if (this._verbose)
                            {
                                Console.WriteLine($"Found entityType {entityType} from Entity {childNode.Attributes["Name"].Value}");
                            }
                            //Console.WriteLine($"{prefix}{node.Name}");
                            if (this._verbose)
                            {
                                Console.WriteLine($"{prefix}{childNode.Name}-{childNode.Attributes["Name"].Value}");
                            }
                        }
                    }
                }
                else if (trim == true
                        && childNode.Name == "Action")
                {
                    // delete all actions
                    nodesToRemove.Add(childNode);
                }

                if (!(childNode.Name == "EntitySet" || childNode.Name == "EntityType")
                    && childNode.HasChildNodes)
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
