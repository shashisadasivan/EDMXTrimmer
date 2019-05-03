# EDMXTrimmer
EDMX trimmer for Dynamics 365 Finance & Operations (D365FO)

Each metadata file can be a very large file out of the box. The resultant class created from ODataClient.tt can be huge and can cause performance issues. in order to reduce the size the Tags *EntityType* and *EntitySet* are removed.
For example a 17MB edmx file can be reduced to 1.3MB by only selecting 1 entity (CustomerV3 & CustomersV3)

## Command line
Download the Executables (as long as you have *.Net Core*)

```
dotnet EDMXTrimmer.dll --edmxfile <your file name here> --entitiesToKeep <entitylist separated by commas>
```

**edmxfile** : This is the OData metadata file that you can download off https://<url>/data/$metadata
Save the ouptut to a file and use it in the command line argument

**entitiesToKeep** : Enter the entity names as a CSV string (plural) E.g. CustomersV3,VendorsV2

### Example
```
dotnet EDMXTrimmer.dll --edmxfile "C:\temp\custODataMetadata.edmx" --entitiesToKeep CustomersV3,VendorsV2
```
