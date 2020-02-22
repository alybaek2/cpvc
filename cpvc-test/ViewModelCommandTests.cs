using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using static CPvC.Test.TestHelpers;

namespace CPvC.Test
{
    public class ViewModelCommandTests
    {
        private class TestObject : INotifyPropertyChanged
        {
            private bool _flag;

            public TestObject(bool flag)
            {
                _flag = flag;
            }

            public bool Flag
            {
                get
                {
                    return _flag;
                }

                set
                {
                    _flag = value;
                    OnPropertyChanged("Flag");
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected void OnPropertyChanged(string name)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Execute(bool canExecute)
        {
            // Setup
            TestObject testObject = new TestObject(false);
            bool executeCalled = false;
            ViewModelCommand command = new ViewModelCommand(
                p => { executeCalled = true; },
                p => { return canExecute; },
                testObject, "Flag");

            // Act
            command.Execute(null);

            // Verify
            Assert.True(executeCalled);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CanExecute(bool canExecute)
        {
            // Setup
            ViewModelCommand command = new ViewModelCommand(
                p => { },
                p => { return canExecute; },
                null, "");

            // Act
            bool result = command.CanExecute(null);

            // Verify
            Assert.AreEqual(canExecute, result);
        }

        [Test]
        public void CanExecuteChanged()
        {
            // Setup
            bool canExecuteChangeCalled = false;
            TestObject testObject = new TestObject(false);
            ViewModelCommand command = new ViewModelCommand(
                p => { },
                p => { return testObject.Flag; },
                testObject, "Flag");
            command.CanExecuteChanged += (sender, e) =>
            {
                canExecuteChangeCalled = true;
            };

            // Act
            testObject.Flag = true;

            // Verify
            Assert.True(canExecuteChangeCalled);
        }
    }
}
