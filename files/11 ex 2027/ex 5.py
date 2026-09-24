import sqlite3
connection = None
try:
    connection = sqlite3.connect('Kuwait_landmarks.db')
    cursor = connection.cursor()
    # Insert data into landmark table
    insert_sql =
    landmarks_list =[('Failaka Island','Off the eastern coast of Kuwait','Historical Landmark',1950,5),
     ('Souq Al-Mubarakiya','Central Kuwait City','Traditional Market',1897,0),
     ('Museum of Modern Art','Salmiya Area','Art Museum',1980,1)]
    cursor.executemany(....,....)
    connection.commit()
except Exception as e:
    print ("Error:",e)
finally:
    if connection is not None:
        connection.close()
