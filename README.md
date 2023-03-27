# TextStats
The text Stats Project can be tested/run in two ways

TextStats.Console
Before generating Statistics,  the following will be prompted
	1. File Path (can be a local disk path or URL)
	2. File Reading Options (1. BufferedStream, 2.BufferedStreamReadLine 3.TextReadLine) Input is either 1 , 2 or 3
	3. Resuse Previous Statistics , Input is Y or N  (if the file is processed earlier (identified by filechecksum) and statistics are avilable returns the statistics )

After statistics is generated, the following options will be prompted.
	1. Longest words 
	2. Top Words
	3. Process another file

TextStats.UnitTest
There are 4 tests that can be run, A sample file is  generated  
The tests validate 3 types of read options
And if the statistics can be reused.