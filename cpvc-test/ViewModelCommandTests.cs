using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using static CPvC.Test.TestHelpers;

namespace CPvC.Test
{
    public class ViewModelCommandTests
    {
        private class TestObject : INotifyPropertyChanged
        {
            private bool _flag;
            private TestObject _child;

            public TestObject(bool flag)
            {
                _flag = flag;
                _child = null;
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

            public TestObject Child
            {
                get
                {
                    return _child;
                }

                set
                {
                    _child = value;
                    OnPropertyChanged("Child");
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
                testObject, "Flag", null);

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
                null, "", null);

            // Act
            bool result = command.CanExecute(null);

            // Verify
            Assert.AreEqual(canExecute, result);
        }
    }
}
