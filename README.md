# EDMXTrimmer
EDMX trimmer for OData specifications.

Some OData specifications (i.e. the metadata or .edmx file) can be a quite large. One example is the specification for the Dynamics 365 Finance & Operations (D365FO) ERP system's OData API.

Tools that operate on OData specifications in turn create huge files. The can also show bad performance or just crash. On example for such a tool is the Visual Studio extension OData Connected Service.

EDMXTrimmer is used to remove entities and related objects (entity sets, navigation properties, actions and enums) from the OData specification that are not needed. 

For example the D365FO OData specification in version 10.0.29 is a 30 MB .edmx file. If only one entity such as CustomersV3 is required, EDMXTrimmer can reduce the file size to less than 200 KB.

## Build
EDMXTrimmer is a .Net Core 3.1 console application. It can be built with Visual Studio 2019 or with the .Net Core 3.1 SDK.

## Command line
EDMXTrimmer can be run from the command line. 

```
dotnet EDMXTrimmer.dll --edmxfile=<your file name here> --entitiestokeep=<entitylist separated by commas>
```

On Windows, you can also run the executable `EDMXTrimmer.exe` instead of `dotnet EDMXTrimmer.dll`.

The following command line arguments are supported:

- **edmxfile** : This is the OData metadata file that you can download off `https://<url>/data/$metadata`. This parameter is required.
Save the output to a file and use it in the command line argument.
- **entitiestokeep** : Enter the entity set names (plural) separated by commas. E.g. `CustomersV3,VendorsV2`. All other entities and their related objects will be removed.
- **enttitiestoexclude** : Enter the entity set names (plural) separated by commas. E.g. `CustomersV3,VendorsV2`. These entites and their related objects will be removed. All other entities will be kept.
- **outputfilename** : The name of the output file. If not specified, the output will be written to file `Output.edmx` in the current directory.
- **entitiesareregularexpressions** : If this parameter is specified, the entity names are treated as regular expressions. This can be used to keep or remove all entities where their names follow a pattern. E.g. `--entitiesToKeep="^(Cust|Vend).+" --entitiesareregularexpressions` will keep all entities that start with Cust or Vend.
- **removeprimaryannotations**: Removes annotations nodes directly under the first schema node.
- **removeactionimports**: Removes action import nodes.

## Examples

### 1) Keep a single entity
```
dotnet EDMXTrimmer.dll --edmxfile="C:\temp\custODataMetadata.edmx" --entitiestokeep=CustomersV3
```
This will keep only the CustomersV3 entity and its related objects. All other entities and their related objects will be removed.

### 2) Specify output file
```
dotnet EDMXTrimmer.dll --edmxfile="C:\temp\custODataMetadata.edmx" --entitiestokeep=CustomersV3 --outputfilename="C:\temp\custODataMetadataTrimmed.edmx"
```
This will keep only the CustomersV3 entity and its related objects. All other entities and their related objects will be removed. The output will be written to the file `C:\temp\custODataMetadataTrimmed.edmx`.

### 3) Keep multiple entities
```
dotnet EDMXTrimmer.dll --edmxfile="C:\temp\custODataMetadata.edmx" --entitiestokeep=CustomersV3,VendorsV2
```
This will keep the CustomersV3 and VendorsV2 entities and their related objects. All other entities and their related objects will be removed.

### 4) Keep entities using wild cards
```
dotnet EDMXTrimmer.dll --edmxfile="C:\temp\custODataMetadata.edmx" --entitiestokeep=Cust*
```
This will keep all entities that start with Cust. All other entities and their related objects will be removed.

### 5) Exclude entities
```
dotnet EDMXTrimmer.dll --edmxfile="C:\temp\custODataMetadata.edmx" --entitiestoexclude=Cust*,VendorsV2
``` 
This will remove all entities that start with Cust and the VendorsV2 entity.

### 6) Exclude entities from included entities
```
dotnet EDMXTrimmer.dll --edmxfile="C:\temp\custODataMetadata.edmx" --entitiestokeep=Cust* --entitiestoexclude=CustomersV2
```
This will first remove all entities that do not start with Cust. From the remaining entities, CustomersV2 will be removed.

Note that `--entitiestoexclude` is applied after `--entitiestokeep`. This means that if you specify both parameters, the entities that are excluded will be removed from the entities that are kept.

### 7) Use regular expressions
```
dotnet EDMXTrimmer.dll --edmxfile "C:\temp\custODataMetadata.edmx" --entitiesToKeep="^(Cust|Vend).+" --entitiesareregularexpressions
```	
This will keep all entities that start with Cust or Vend. All other entities will be removed.

Note that this could also be achieved with wild cards: `--entitiestokeep=Cust*,Vend*`. The regular expression is only needed if you want to use more complex patterns.

### 8) Remove annotations and action imports
```
dotnet EDMXTrimmer.dll --edmxfile "C:\temp\custODataMetadata.edmx" --removeprimaryannotations --removeactionimports
```
This will remove all primary annotations and action import nodes from the edmx file.
