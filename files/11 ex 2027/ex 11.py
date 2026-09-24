import sqlite3
# Connect to the database
connection = sqlite3.connect ('Kuwait_landmarks.db')
cursor = connection.cursor()
# Delete Data
cursor.execute (



  
)
print ("Number of rows deleted:",cursor.rowcount)
# Commit changes
connection.commit()
# Close database
connection.close()



