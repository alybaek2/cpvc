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

        [TestCase(false)]
        [TestCase(true)]
        public void CanExecuteChanged(bool subscribe)
        {
            // Setup
            bool canExecuteChangeCalled = false;
            TestObject testObject = new TestObject(false);
            ViewModelCommand command = new ViewModelCommand(
                p => { },
                p => { return testObject.Flag; },
                testObject, "Flag", null);
            if (subscribe)
            {
                command.CanExecuteChanged += (sender, e) =>
                {
                    canExecuteChangeCalled = true;
                };
            }

            // Act
            Assert.DoesNotThrow(() => testObject.Flag = true);

            // Verify
            Assert.AreEqual(subscribe, canExecuteChangeCalled);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CanExecuteChangedChild(bool subscribe)
        {
            // Setup
            bool canExecuteChangeCalled = false;
            TestObject testObject = new TestObject(false);
            TestObject testObjectChild = new TestObject(false);
            testObject.Child = testObjectChild;
            ViewModelCommand command = new ViewModelCommand(
                p => { },
                p => { return testObject.Flag; },
                testObject, "Child", "Flag");
            if (subscribe)
            {
                command.CanExecuteChanged += (sender, e) =>
                {
                    canExecuteChangeCalled = true;
                };
            }

            // Act
            Assert.DoesNotThrow(() =>
            {
                TestObject testObjectChild2 = new TestObject(false);
                testObject.Child = testObjectChild2;
            });

            // Verify
            Assert.AreEqual(subscribe, canExecuteChangeCalled);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CanExecuteChangedChildFlag(bool subscribe)
        {
            // Setup
            bool canExecuteChangeCalled = false;
            TestObject testObject = new TestObject(false);
            TestObject testObjectChild = new TestObject(false);
            testObject.Child = testObjectChild;
            ViewModelCommand command = new ViewModelCommand(
                p => { },
                p => { return testObject.Flag; },
                testObject, "Child", "Flag");
            if (subscribe)
            {
                command.CanExecuteChanged += (sender, e) =>
                {
                    canExecuteChangeCalled = true;
                };
            }

            // Act
            Assert.DoesNotThrow(() => testObject.Child.Flag = true);

            // Verify
            Assert.AreEqual(subscribe, canExecuteChangeCalled);
        }
    }
}
