import sqlite3
# Connect to the database
connection = sqlite3.connect('Kuwait_landmarks.db')
cursor = connection.cursor()
# Data Retrieval
cursor.execute(                )
rows = cursor.fetchall()
for row in rows:

# Close the database
connection.close()
