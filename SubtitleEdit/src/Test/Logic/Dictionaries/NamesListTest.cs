using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nikse.SubtitleEdit.Logic.Dictionaries;
using System.IO;

namespace Test.Logic.Dictionaries
{
    [TestClass]
    public class NamesListTest
    {
        [TestMethod]
        public void NamesListAddWord()
        {
            // Arrange
            var namesList = new NamesList(Directory.GetCurrentDirectory(), "en", false, null);

            // Act
            namesList.Add("Jones");
            var exists = namesList.GetNames().Contains("Jones");

            // Assert
            Assert.IsTrue(exists);
        }

        [TestMethod]
        public void NamesListAddMultiWord()
        {
            // Arrange
            var namesList = new NamesList(Directory.GetCurrentDirectory(), "en", false, null);

            // Act
            namesList.Add("Kremena you have dandruff on your shoes, think about that.");

            var exists = namesList.GetMultiNames().Contains("Kremena you have dandruff on your shoes, think about that.");

            // Assert
            Assert.IsTrue(exists);    
        }

        [TestMethod]
        public void NamesListIsInNamesEtcMultiWordList()
        {
            // Arrange
            var namesList = new NamesList(Directory.GetCurrentDirectory(), "en", false, null);

            // Act
            namesList.Add("Charlie Parker");
            var exists = namesList.IsInNamesEtcMultiWordList("This is Charlie Parker!", "Charlie Parker");

            // Assert
            Assert.IsTrue(exists);
        }

        [TestMethod]
        public void NamesListNotInList()
        {
            // Arrange
            var namesList = new NamesList(Directory.GetCurrentDirectory(), "en", false, null);

            // Act
            namesList.Add("Gosho");
            namesList.Add("Pesho");
            namesList.Add("Nashmat");

            var exists = namesList.GetNames().Contains("Ivan");

            // Assert
            Assert.IsFalse(exists);    
        }

        [TestMethod]
        public void NamesListAddWordReload()
        {
            // Arrange
            var namesList = new NamesList(Directory.GetCurrentDirectory(), "en", false, null);
            namesList.Add("Jones");

            // Act
            namesList = new NamesList(Directory.GetCurrentDirectory(), "en", false, null);

            // Assert
            Assert.IsTrue(namesList.GetNames().Contains("Jones"));
        }

        [TestMethod]
        public void NamesListAddTextThatDontContainsLetter()
        {
            // Arrange
            bool isItAdded;
            var namesList = new NamesList(Directory.GetCurrentDirectory(), "en", false, null);

            // Act
            isItAdded = namesList.Add("~");

            // Assert
            Assert.IsFalse(isItAdded);
        }

        [TestMethod]
        public void NamesListRemove()
        {
            // Arrange
            var namesList = new NamesList(Directory.GetCurrentDirectory(), "en", false, null);
            namesList.Add("Jones");

            // Act
            namesList.Remove("Jones");

            // Assert
            Assert.IsFalse(namesList.GetNames().Contains("Jones"));
        }

        [TestMethod]
        public void NamesListRemove_WithLetter()
        {
            // Arrange
            bool isItRemoved;
            var namesList = new NamesList(Directory.GetCurrentDirectory(), "en", false, null);

            // Act
            isItRemoved = namesList.Remove("a");

            // Assert
            Assert.IsFalse(isItRemoved);
        }

        [TestMethod]
        public void NamesListRemoveReload()
        {
            // Arrange
            var namesList = new NamesList(Directory.GetCurrentDirectory(), "en", false, null);
            namesList.Add("Goshko");
            namesList.Add("Ivan");

            // Act
            namesList.Remove("Goshko");
            namesList = new NamesList(Directory.GetCurrentDirectory(), "en", false, null);

            // Assert
            Assert.IsFalse(namesList.GetNames().Contains("Goshko"));
        }
    }
}
