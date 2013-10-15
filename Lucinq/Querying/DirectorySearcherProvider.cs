﻿using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucinq.Interfaces;

namespace Lucinq.Querying
{
	public class DirectorySearcherProvider : IIndexSearcherProvider
    {
        #region [ Fields ]

        private IndexSearcher indexSearcher;

	    private readonly Directory currentDirectory;

        #endregion

        #region [ Constructors ]

	    public DirectorySearcherProvider(Directory luceneDirectory, bool closesDirectory)
	    {
            currentDirectory = luceneDirectory;
	        ClosesDirectory = closesDirectory;
	    }

        public DirectorySearcherProvider(Directory luceneDirectory) : this(luceneDirectory, true)
		{

		}

        #endregion

        #region [ Methods ]

        public void Dispose()
		{
		    if (indexSearcher == null)
		    {
		        return;
		    }
			indexSearcher.Dispose();
		    indexSearcher = null;
		    // Cannot dispose of this, as it doesn't work.
            if (ClosesDirectory)
            {
                currentDirectory.Dispose();  
            }
		}

        #endregion

        #region [ Properties ]

        public IndexSearcher IndexSearcher
	    {
	        get { return indexSearcher ?? (indexSearcher = new IndexSearcher(currentDirectory, true)); }
        }

	    public bool ClosesDirectory { get; private set; }

	    #endregion
    }
}
