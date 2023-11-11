using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EDMXTrimmer;

namespace EDMXTrimmer.Tests
{
    public class EdmxTrimmerTests
    {
        private const string TestEdmxFile = "TripPin.xml";
        private const string TestOutputFile = "TripPin-Trimmed.xml";

        [SetUp]
        public void Setup()
        {
            
        }

        [TearDown]
        public void TearDown()
        {
            // Delete the test EDMX file and output file
            File.Delete(TestOutputFile);
        }

        [Test]
        public void TestEdmxTrimmer()
        {
            // Arrange
            var entitiesToKeep = new List<string> { "Photos", "People" };
            var entitiesToExclude = new List<string> { "People" };
            var edmxTrimmer = new EdmxTrimmer(TestEdmxFile, TestOutputFile, true, entitiesToKeep, entitiesToExclude);

            // Act
            edmxTrimmer.AnalyzeFile();

            // Assert
            Assert.That(File.Exists(TestOutputFile), Is.True);

            var trimmedEdmx = File.ReadAllText(TestOutputFile);
            Assert.Multiple(() =>
            {
                Assert.That(trimmedEdmx, Does.Contain("<EntitySet Name=\"Photos\""));
                Assert.That(trimmedEdmx, Does.Not.Contain("<EntitySet Name=\"People\""));
                Assert.That(trimmedEdmx, Does.Not.Contain("<EntitySet Name=\"Airlines\""));
                Assert.That(trimmedEdmx, Does.Not.Contain("<EntitySet Name=\"Airports\""));
            });
        }

        [Test]
        public void TestEdmxTrimmer_NoEntitiesToKeepOrExclude()
        {
            // Arrange
            var edmxTrimmer = new EdmxTrimmer(TestEdmxFile, TestOutputFile);

            // Act
            edmxTrimmer.AnalyzeFile();

            // Assert
            Assert.That(File.Exists(TestOutputFile), Is.True);

            var trimmedEdmx = File.ReadAllText(TestOutputFile);
            Assert.That(trimmedEdmx, Is.EqualTo(File.ReadAllText(TestEdmxFile)));
        }

        [Test]
        public void TestEdmxTrimmer_RemovePrimaryAnnotations()
        {
            // Arrange
            var edmxTrimmer = new EdmxTrimmer(TestEdmxFile, TestOutputFile, removePrimaryAnnotations: true);

            // Act
            edmxTrimmer.AnalyzeFile();

            // Assert
            Assert.That(File.Exists(TestOutputFile), Is.True);

            var trimmedEdmx = File.ReadAllText(TestOutputFile);
            Assert.That(trimmedEdmx, Does.Not.Contain("Primary"));
        }

        [Test]
        public void TestEdmxTrimmer_RemoveActionImports()
        {
            // Arrange
            var edmxTrimmer = new EdmxTrimmer(TestEdmxFile, TestOutputFile, removeActionImports: true);

            // Act
            edmxTrimmer.AnalyzeFile();

            // Assert
            Assert.That(File.Exists(TestOutputFile), Is.True);

            var trimmedEdmx = File.ReadAllText(TestOutputFile);
            Assert.That(trimmedEdmx, Does.Not.Contain("ActionImport"));
        }

        [Test]
        public void TestEdmxTrimmer_RemoveFunctionImports()
        {
            // Arrange
            var edmxTrimmer = new EdmxTrimmer(TestEdmxFile, TestOutputFile)
            {
                RemoveFunctionImportsFlag = true
            };
            

            // Act
            edmxTrimmer.AnalyzeFile();

            // Assert
            Assert.That(File.Exists(TestOutputFile), Is.True);

            var trimmedEdmx = File.ReadAllText(TestOutputFile);
            Assert.That(trimmedEdmx, Does.Not.Contain("FunctionImport"));
        }

        [Test]
        public void TestEdmxTrimmer_RemoveComplexTypes()
        {
            // Arrange
            var edmxTrimmer = new EdmxTrimmer(TestEdmxFile, TestOutputFile)
            {
                RemoveComplexTypesFlag = true
            };

            // Act
            edmxTrimmer.AnalyzeFile();

            // Assert
            Assert.That(File.Exists(TestOutputFile), Is.True);

            var trimmedEdmx = File.ReadAllText(TestOutputFile);
            Assert.That(trimmedEdmx, Does.Not.Contain("ComplexType"));
        }

        [Test]
        public void TestEdmxTrimmer_EntitiesAreRegularExpressions()
        {
            // Arrange

            var entitiesToKeep = new List<string> { @"\b\w+s\b" }; // Keep entities that end with "s"
            var entitiesToExclude = new List<string> { @"P\w+" }; // Exclude entities that start with "P"
            var edmxTrimmer = new EdmxTrimmer(
                TestEdmxFile, 
                TestOutputFile, 
                entitiesAreRegularExpressions: true, 
                entitiesToKeep: entitiesToKeep, 
                entitiesToExclude: entitiesToExclude);

            // Act
            edmxTrimmer.AnalyzeFile();

            // Assert
            Assert.That(File.Exists(TestOutputFile), Is.True);

            var trimmedEdmx = File.ReadAllText(TestOutputFile);
            Assert.Multiple(() =>
            {
                Assert.That(trimmedEdmx, Does.Not.Contain("<EntitySet Name=\"Photos\""));
                Assert.That(trimmedEdmx, Does.Not.Contain("<EntitySet Name=\"People\""));
                Assert.That(trimmedEdmx, Does.Contain("<EntitySet Name=\"Airlines\""));
                Assert.That(trimmedEdmx, Does.Contain("<EntitySet Name=\"Airports\""));
            });
        }
    }
}