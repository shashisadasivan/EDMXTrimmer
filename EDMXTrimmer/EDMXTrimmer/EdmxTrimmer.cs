using System;
using System.Collections.Generic;
using System.Xml;
using System.Linq;
using System.Text.RegularExpressions;

namespace EDMXTrimmer
{
    class EdmxTrimmer
    {
        public string EdmxFile { get; private set; }
        public bool Verbose { get; private set; }
        public List<string> EntitiesToKeep { get; private set; }
        public List<string> EntitiesToExclude { get; private set; }
        public bool EntitiesAreRegularExpressions { get; private set; }
        public bool RemovePrimaryAnnotationsFlag { get; private set; }
        public bool RemoveActionImportsFlag { get; private set; }
        public string OutputFileName { get; set; }

        private XmlDocument _xmlDocument;
        private XmlNode _firstSchemaNode;
        private string ENTITYNAMESPACE;
        private string ENTITYNAMESPACE_ALIAS;
        private const string TAG_SCHEMA = "Schema";
        private const string TAG_ENTITY_TYPE = "EntityType";
        private const string TAG_ENTITY_SET = "EntitySet";
        private const string TAG_NAVIGATION_PROPERTY = "NavigationProperty";
        private const string TAG_ACTION = "Action";
        private const string TAG_ANNOTATIONS = "Annotations";
        private const string TAG_PROPERTY = "Property";
        private const string TAG_RETURN_TYPE = "ReturnType";
        private const string TAG_PARAMETER = "Parameter";
        private const string TAG_ENUM_TYPE = "EnumType";
        private const string TAG_ACTION_IMPORT = "ActionImport";
        private const string ATTRIBUTE_ALIAS = "Alias";
        private const string ATTRIBUTE_NAMESPACE = "Namespace";
        private const string ATTRIBUTE_NAME = "Name";
        private const string ATTRIBUTE_TYPE = "Type";
        private const string ATTRIBUTE_TARGET = "Target";
        private const string ATTRIBUTE_AXType = "AXType";

        public EdmxTrimmer(
            string edmxFile, 
            string outputFileName,
            bool verbose = true,
            List<String> entitiesToKeep = null,
            List<String> entitiesToExclude = null,
            bool entitiesAreRegularExpressions = false,
            bool removePrimaryAnnotations = false,
            bool removeActionImports = false)
        {
            this.EdmxFile = edmxFile;
            this.Verbose = verbose;
            this.OutputFileName = outputFileName;

            this.EntitiesToKeep = new List<string>();
            if (entitiesToKeep != null && entitiesToKeep.Count > 0)
            {
                this.EntitiesToKeep.AddRange(entitiesToKeep);
            }

            this.EntitiesToExclude = new List<string>();
            if (entitiesToExclude != null && entitiesToExclude.Count > 0)
            {
                this.EntitiesToExclude.AddRange(entitiesToExclude);
            }

            this.EntitiesAreRegularExpressions = entitiesAreRegularExpressions;
            this.RemovePrimaryAnnotationsFlag = removePrimaryAnnotations;
            this.RemoveActionImportsFlag = removeActionImports;

            this.LoadFile();
        }

        private void LoadFile()
        {
            this._xmlDocument = new XmlDocument();
            this._xmlDocument.Load(this.EdmxFile);
        }

        public void AnalyzeFile()
        {
            this._firstSchemaNode = this._xmlDocument.GetElementsByTagName(TAG_SCHEMA)[0];
            this.ENTITYNAMESPACE = this._firstSchemaNode.Attributes[ATTRIBUTE_NAMESPACE].Value + ".";
            
            var aliasAttrValue = this._firstSchemaNode.Attributes[ATTRIBUTE_ALIAS]?.Value.Trim();
            if(!string.IsNullOrEmpty(aliasAttrValue)) {
                this.ENTITYNAMESPACE_ALIAS = aliasAttrValue + ".";
            }

            var entitySets = this._xmlDocument.GetElementsByTagName(TAG_ENTITY_SET).Cast<XmlNode>().ToList();
            var entityTypes = this._xmlDocument.GetElementsByTagName(TAG_ENTITY_TYPE).Cast<XmlNode>().ToList();
            var originalEntityCount = entitySets.Count;

            if (this.EntitiesToKeep.Count > 0)
            {
                RemoveAllEntitiesExcept(this.EntitiesToKeep, entitySets, entityTypes);
            }

            if (this.EntitiesToExclude.Count > 0)
            {
                if (this.EntitiesToKeep.Count > 0)
                {
                    // Update entity sets and types with the remaining elements so that it does not try to remove entities that have already been removed
                    entitySets = this._xmlDocument.GetElementsByTagName(TAG_ENTITY_SET).Cast<XmlNode>().ToList();
                    entityTypes = this._xmlDocument.GetElementsByTagName(TAG_ENTITY_TYPE).Cast<XmlNode>().ToList();
                }
                RemoveExcludedEntities(this.EntitiesToExclude, entitySets, entityTypes);
            }

            if (this.RemovePrimaryAnnotationsFlag)
            {
                RemovePrimaryAnnotations();
            }
            if (this.RemoveActionImportsFlag)
            {
                RemoveActionImports();
            }

            this._xmlDocument.Save(OutputFileName);
            Console.WriteLine($"Trimmed EDMX saved to file: {OutputFileName}");
            if (Verbose)
            {
                entitySets = this._xmlDocument.GetElementsByTagName(TAG_ENTITY_SET).Cast<XmlNode>().ToList();
                Console.WriteLine($"Original number of entities: {originalEntityCount}");
                Console.WriteLine($"Number of remaining entities: {entitySets.Count}");
            }
        }

        private void RemoveAllEntitiesExcept(
            List<string> entitiesToKeep, 
            List<XmlNode> entitySets, 
            List<XmlNode> entityTypes)
        {
            string regex = EntitySearchTermsToRegularExpression(entitiesToKeep);
            var entitiesKeep = entitySets.Where(n => Regex.IsMatch(n.Attributes[ATTRIBUTE_NAME].Value, regex)).ToList();

            RemoveEntitySets(entitySets, entitiesKeep);
            RemoveEntityTypes(entityTypes, entitiesKeep);
        }

        private void RemoveExcludedEntities(
            List<string> entitiesToExclude, 
            List<XmlNode> entitySets, 
            List<XmlNode> entityTypes)
        {
            string regex = EntitySearchTermsToRegularExpression(entitiesToExclude);
            var entitiesKeep = entitySets.Where(n => !Regex.IsMatch(n.Attributes[ATTRIBUTE_NAME].Value, regex)).ToList();

            RemoveEntitySets(entitySets, entitiesKeep);
            RemoveEntityTypes(entityTypes, entitiesKeep);
        }

        private string EntitySearchTermsToRegularExpression(List<string> entitiesToKeep)
        {
            List<string> listRegularExpression = entitiesToKeep.Select(s => EntitySearchTermToRegularExpression(s)).ToList();
            string regex = String.Join("|", listRegularExpression.ToArray());

            return regex;
        }

        private string EntitySearchTermToRegularExpression(string searchTerm)
        {
            string regex = searchTerm;
            if (this.EntitiesAreRegularExpressions == false)
            {
                regex = "^" + Regex.Escape(searchTerm)
                    .Replace("\\?", ".")
                    .Replace("\\*", ".*") + "$";
            }
            return regex;
        }

        private void RemoveEntitySets(List<XmlNode> entitySets, List<XmlNode> entitiesKeep)
        {
            // Remove entities not required (EntitySet)
            entitySets.Except(entitiesKeep).ToList().ForEach(n => n.ParentNode.RemoveChild(n));
            //Remove unwanted Nodes in the Entity Set
            entitiesKeep.ForEach(n =>
            {
                // Remove Node NavigationProperty
                var navProperties = n.ChildNodes.Cast<XmlNode>()
                    .Where(navProp => navProp.Name.Equals(TAG_NAVIGATION_PROPERTY)).ToList();
                navProperties
                    .ForEach(navProp => navProp.ParentNode.RemoveChild(navProp));
            });
        }

        private void RemoveEntityTypes(List<XmlNode> entityTypes, List<XmlNode> entitiesKeep)
        {
            List<String> entityTypesFound = new List<string>();
            entitiesKeep.ForEach(n => {
                var entityType = GetEntityTypeWithoutNamespace(n);
                entityTypesFound.Add(entityType);
            });

            // Remove all navigation properties
            this._xmlDocument.GetElementsByTagName(TAG_NAVIGATION_PROPERTY).Cast<XmlNode>()
                .Where(navProp => !entityTypesFound.Any(entityType => EntityExists(navProp, entityType))).ToList()
                .ForEach(n => n.ParentNode.RemoveChild(n));

            // Remove entity not required (EntityType)
            var entityTypesToKeep = entityTypes.Where(n => entityTypesFound.Contains(n.Attributes[ATTRIBUTE_NAME].Value)).ToList();
            entityTypes.Except(entityTypesToKeep).ToList().ForEach(n => n.ParentNode.RemoveChild(n));
            
            // Remove all Actions         
            this._xmlDocument.GetElementsByTagName(TAG_ACTION).Cast<XmlNode>()
                .Where(action => !entityTypesFound.Any(entityType => action.ChildNodes.Cast<XmlNode>().
                    Any(childNode => EntityExists(childNode, entityType)))).ToList()
                .ForEach(n => n.ParentNode.RemoveChild(n));

            // Determine enums to keep
            List<String> enumTypesFound = new List<string>();
            // Enums from entity type properties

            var propertiesTypes = entityTypesToKeep.SelectMany(typeNode => GetEntityTypesFromNodeChildren(typeNode, TAG_PROPERTY));
            enumTypesFound.AddRange(propertiesTypes);

            // Enums from actions  
            var entityActions = this._xmlDocument.GetElementsByTagName(TAG_ACTION).Cast<XmlNode>().ToList();     
            entityActions.ForEach(actionNode =>
            {
                // Enums from parameters
                var parametersTypes = GetEntityTypesFromNodeChildren(actionNode, TAG_PARAMETER)!;
                enumTypesFound.AddRange(parametersTypes);

                // Enum from return type
                // get the first child node with name "ReturnType" if it exists
                var returnType = GetEntityTypesFromNodeChildren(actionNode, TAG_RETURN_TYPE)!;
                enumTypesFound.Add(returnType.FirstOrDefault());
            });
            // Remove unused Enums except AXType
            this._xmlDocument.GetElementsByTagName(TAG_ENUM_TYPE).Cast<XmlNode>()
                .Where(enumType => 
                    !enumType.Attributes[ATTRIBUTE_NAME].Value.Equals(ATTRIBUTE_AXType)
                    && !enumTypesFound.Contains(enumType.Attributes[ATTRIBUTE_NAME].Value)).ToList()
                .ForEach(n => n.ParentNode.RemoveChild(n));

            this._xmlDocument.Save(OutputFileName);
            
            return;

            string GetEntityTypeWithoutNamespace(XmlNode n) {
                var entityType = n.Attributes[TAG_ENTITY_TYPE]?.Value;

                if(ENTITYNAMESPACE_ALIAS != null) {
                    var replaced = entityType.Replace(ENTITYNAMESPACE_ALIAS, "");
                    if(replaced != entityType) {
                        return replaced;
                    }
                }

                return entityType.Replace(ENTITYNAMESPACE, "");
            }

            IEnumerable<string> GetEntityTypesFromNodeChildren(XmlNode typeNode, string nodeName) =>
                typeNode
                    .ChildNodes
                    .Cast<XmlNode>()
                    .Where(prop => prop.Name.Equals(nodeName))
                    .Select(RemoveNamespace)
                    .Where(name => name != null);

            string RemoveNamespace(XmlNode xmlNode) {
                var enumType = xmlNode.Attributes[ATTRIBUTE_TYPE]?.Value;

                if(enumType == null) {
                    return null;
                }
                if(ENTITYNAMESPACE_ALIAS != null && enumType.StartsWith(ENTITYNAMESPACE_ALIAS)) {
                    return enumType.Replace(ENTITYNAMESPACE_ALIAS, "");
                }
                if(enumType.StartsWith(ENTITYNAMESPACE)) {
                    return enumType.Replace(ENTITYNAMESPACE, "");
                }

                return null;
            }
        }

        private void RemovePrimaryAnnotations()
        {
            this._firstSchemaNode.ChildNodes.Cast<XmlNode>()
                .Where(annotationsNode => 
                    annotationsNode.Name.Equals(TAG_ANNOTATIONS)
                    && annotationsNode.Attributes[ATTRIBUTE_TARGET].Value.StartsWith(ENTITYNAMESPACE)).ToList()
                .ForEach(n => n.ParentNode.RemoveChild(n));
        }

        private void RemoveActionImports()
        {
            this._xmlDocument.GetElementsByTagName(TAG_ACTION_IMPORT).Cast<XmlNode>()
                .ToList()
                .ForEach(n => n.ParentNode.RemoveChild(n));
        }

        private bool EntityExists(XmlNode xmlNode, string entityType) {
            var typeValue = xmlNode.Attributes[ATTRIBUTE_TYPE]?.Value;

            if(null == typeValue) {
                return false;
            }
            if(ENTITYNAMESPACE_ALIAS != null && Regex.IsMatch(typeValue, Regex.Escape(ENTITYNAMESPACE_ALIAS + entityType) + "\\)?$")) {
                return true;
            }
            return Regex.IsMatch(typeValue, Regex.Escape(ENTITYNAMESPACE + entityType) + "\\)?$");
        }
    }
}