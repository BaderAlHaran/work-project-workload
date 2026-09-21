import sqlite3
connection = sqlite3.connect('Kuwait_landmarks.db')
cursor = connection.cursor()
# Update Data
cursor.execute(

    
)
print ("Number of rows updated:",connection.total_changes)
connection.commit()
connection.close()

