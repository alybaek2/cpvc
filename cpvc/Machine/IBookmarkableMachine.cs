﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public interface IBookmarkableMachine
    {
        void AddBookmark(bool system);
        void SeekToLastBookmark();
    }
}
