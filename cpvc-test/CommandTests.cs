using NUnit.Framework;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CPvC.Test
{
    public class CommandTests
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
                    OnPropertyChanged();
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
                    OnPropertyChanged();
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected void OnPropertyChanged([CallerMemberName] string name = null)
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
            Command command = new Command(
                p => { executeCalled = true; },
                p => { return canExecute; });

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
            Command command = new Command(
                p => { },
                p => { return canExecute; });

            // Act
            bool result = command.CanExecute(null);

            // Verify
            Assert.AreEqual(canExecute, result);
        }
    }
}
