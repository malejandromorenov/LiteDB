﻿using LiteDB.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    internal partial class DbEngine : IDisposable
    {
        /// <summary>
        /// Returns all collection inside datafile
        /// </summary>
        public IEnumerable<string> GetCollectionNames()
        {
            lock(_locker)
            {
                _transaction.AvoidDirtyRead();

                var header = _pager.GetPage<HeaderPage>(0);

                return header.CollectionPages.Keys.AsEnumerable();
            }
        }

        /// <summary>
        /// Drop collection including all documents, indexes and extended pages
        /// </summary>
        public bool DropCollection(string colName)
        {
            return this.Transaction<bool>(colName, false, (col) =>
            {
                if(col == null) return false;

                _log.Write(Logger.COMMAND, "drop collection {0}", colName);

                _collections.Drop(col);

                return true;
            });
        }

        /// <summary>
        /// Rename a collection
        /// </summary>
        public bool RenameCollection(string colName, string newName)
        {
            return this.Transaction<bool>(colName, false, (col) =>
            {
                if(col == null) return false;

                _log.Write(Logger.COMMAND, "rename collection '{0}' -> '{1}'", colName, newName);

                // check if newName already exists
                if(this.GetCollectionNames().Contains(newName.Trim().ToLower()))
                {
                    throw LiteException.AlreadyExistsCollectionName(newName);
                }

                // set page as dirty before any change
                _pager.SetDirty(col);

                // change collection name and save
                col.CollectionName = newName;

                // update header collection reference
                var header = _pager.GetPage<HeaderPage>(0, true);

                header.CollectionPages.Remove(colName.Trim().ToLower());
                header.CollectionPages.Add(newName.Trim().ToLower(), col.PageID);

                return true;
            });
        }
    }
}