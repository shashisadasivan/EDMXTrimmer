using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Linq;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace EDMXTrimmer
{
    class EdmxTrimmer
    {
        public string EdmxFile { get; private set; }
        public bool Verbose { get; private set; }
        public List<string> EntitiesToKeep { get; private set; }
        public string OutputFileName { get; set; }

        private XmlDocument _xmlDocument;
        private const string ENTITY_TYPE = "EntityType";
        private const string ENTITY_SET = "EntitySet";
        private const string NAVIGATION_PROPERTY = "NavigationProperty";
        private const string ACTION = "Action";
        private const string ATTRIBUTE_NAME = "Name";
        private const string ATTRIBUTE_TYPE = "Type";
        private const string ATTRIBUTE_RETURN_TYPE = "ReturnType";
        private const string ENTITYNAMESPACE = "Microsoft.Dynamics.DataEntities.";

        public EdmxTrimmer(string edmxFile, string outputFileName,  bool verbose = true, List<String> entitiesToKeep = null)
        {
            this.EdmxFile = edmxFile;
            this.Verbose = verbose;
            this.OutputFileName = outputFileName;

            this.EntitiesToKeep = new List<string>();
            if(entitiesToKeep != null && entitiesToKeep.Count > 0)
            {
                this.EntitiesToKeep.AddRange(entitiesToKeep);
            }

            this.LoadFile();
        }

        private void LoadFile()
        {
            this._xmlDocument = new XmlDocument();
            this._xmlDocument.Load(this.EdmxFile);
        }

        public void AnalyzeFile()
        {
            var entitySets = this._xmlDocument.GetElementsByTagName(ENTITY_SET).Cast<XmlNode>().ToList();
            var entityTypes = this._xmlDocument.GetElementsByTagName(ENTITY_TYPE).Cast<XmlNode>().ToList();

            List<String> entityTypesFound = new List<string>();
            //if (this.Verbose)
            //{
            //    // Print list of ALL entities
            //    entitySets.ForEach(n => Console.WriteLine(n.Attributes[ATTRIBUTE_NAME].Value));
            //}

            var entitiesKeep = entitySets.Where(n => this.EntitiesToKeep.Contains(n.Attributes[ATTRIBUTE_NAME].Value)).ToList();
            entitiesKeep.ForEach(n =>
            {
                string entityType = n.Attributes[ENTITY_TYPE].Value;
                entityType = entityType.Replace(ENTITYNAMESPACE, "");
                entityTypesFound.Add(entityType);
            });

            if (this.Verbose)
            {
                Console.WriteLine("Entity definitions found:");
                entityTypesFound.ForEach(n => Console.WriteLine(n));
            }

            // Remove entities not required (EntitySet)
            entitySets.Except(entitiesKeep).ToList().ForEach(n => n.ParentNode.RemoveChild(n));
            //Remove unwanted Nodes in the Entity Set
            entitiesKeep.ForEach(n =>
            {
                // Remove Node NavigationProperty
                var navProperties = n.ChildNodes.Cast<XmlNode>()
                    .Where(navProp => navProp.Name.Equals(NAVIGATION_PROPERTY)).ToList();
                navProperties
                    .ForEach(navProp => navProp.ParentNode.RemoveChild(navProp));
            });
            
            // Remove all navigation properties
            this._xmlDocument.GetElementsByTagName(NAVIGATION_PROPERTY).Cast<XmlNode>()
                .Where(navProp => !entityTypesFound.Any(entityType => EntityExists(navProp, entityType))).ToList()
                .ForEach(n => n.ParentNode.RemoveChild(n));

            // Remove entity not required (EntityType)
            var entityTypesKeep = entityTypes.Where(n => entityTypesFound.Contains(n.Attributes[ATTRIBUTE_NAME].Value)).ToList();
            entityTypes.Except(entityTypesKeep).ToList().ForEach(n => n.ParentNode.RemoveChild(n));

            // Remove all Actions         
            this._xmlDocument.GetElementsByTagName(ACTION).Cast<XmlNode>()
                .Where(action => !entityTypesFound.Any(entityType => action.ChildNodes.Cast<XmlNode>().
                    Any(childNode => EntityExists(childNode, entityType)))).ToList()
                .ForEach(n => n.ParentNode.RemoveChild(n));

            // Determine enums to keep
            List<String> enumTypesFound = new List<string>();
            // Enums from entity type properties
            entityTypesKeep.ForEach(n =>
            {
                var properties = n.ChildNodes.Cast<XmlNode>().Where(prop => prop.Name.Equals("Property")).ToList();
                properties.ForEach(prop =>
                {
                    if (prop.Attributes[ATTRIBUTE_TYPE] != null)
                    {
                        var enumType = prop.Attributes[ATTRIBUTE_TYPE].Value;
                        if (enumType.StartsWith("Microsoft.Dynamics.DataEntities."))
                        {
                            enumType = enumType.Replace("Microsoft.Dynamics.DataEntities.", "");
                            enumTypesFound.Add(enumType);
                        }
                    }
                });
            });
            // Enums from actions  
            var entityActions = this._xmlDocument.GetElementsByTagName(ACTION).Cast<XmlNode>().ToList();     
            entityActions.ForEach(action =>
            {
                // Enums from parameters
                var parameters = action.ChildNodes.Cast<XmlNode>().Where(param => param.Name.Equals("Parameter")).ToList();
                parameters.ForEach(param =>
                {
                    if (param.Attributes[ATTRIBUTE_TYPE] != null)
                    {
                        var enumType = param.Attributes[ATTRIBUTE_TYPE].Value;
                        if (enumType.StartsWith("Microsoft.Dynamics.DataEntities."))
                        {
                            enumType = enumType.Replace("Microsoft.Dynamics.DataEntities.", "");
                            enumTypesFound.Add(enumType);
                        }
                    }
                });
                // Enum from return type
                // get the first child node with name "ReturnType" if it exists
                var returnType = action.ChildNodes.Cast<XmlNode>().FirstOrDefault(node => node.Name.Equals("ReturnType"));
                if (returnType != null && returnType.Attributes[ATTRIBUTE_TYPE] != null)
                {
                    var enumType = returnType.Attributes[ATTRIBUTE_TYPE].Value;
                    if (enumType.StartsWith("Microsoft.Dynamics.DataEntities."))
                    {
                        enumType = enumType.Replace("Microsoft.Dynamics.DataEntities.", "");
                        enumTypesFound.Add(enumType);
                    }
                }
                
            });
            // Remove unused Enums except AXType
            this._xmlDocument.GetElementsByTagName("EnumType").Cast<XmlNode>()
                .Where(enumType => 
                    !enumType.Attributes[ATTRIBUTE_NAME].Value.Equals("AXType")
                    && !enumTypesFound.Contains(enumType.Attributes[ATTRIBUTE_NAME].Value)).ToList()
                .ForEach(n => n.ParentNode.RemoveChild(n));

            this._xmlDocument.Save(OutputFileName);
            if(this.Verbose)
            {
                Console.WriteLine($"EDMX Saved to file: {OutputFileName}");
            }

        }
        private bool EntityExists(XmlNode xmlNode, string entityType)
        {
            return xmlNode.Attributes[ATTRIBUTE_TYPE] == null ? false : Regex.IsMatch(xmlNode.Attributes[ATTRIBUTE_TYPE].Value, ENTITYNAMESPACE + entityType + "\\)?$");
        }
    }
}
