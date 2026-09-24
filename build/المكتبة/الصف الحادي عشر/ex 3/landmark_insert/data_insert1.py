import sqlite3
# Connect to the database
connection = sqlite3.connect('Kuwait_landmarks.db')
cursor = connection.cursor()
# Insert data into table book
cursor.execute(


)
# Commit changes
connection.commit()
# Close database
connection.close()
