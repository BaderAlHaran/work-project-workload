import sqlite3

def setup_databases():
    # --- 1. إعداد قاعدة بيانات الكتب (grade11_books.db) ---
    print("جاري تهيئة قاعدة بيانات الكتب...")
    conn1 = sqlite3.connect(r'../../grade11_books.db')
    cursor1 = conn1.cursor()

    # حذف الجداول السابقة
    cursor1.execute('DROP TABLE IF EXISTS books')

    # وضع category قبل year وحذف cover
    cursor1.execute('''
    CREATE TABLE books (
        id INTEGER PRIMARY KEY,
        title TEXT NOT NULL,
        author TEXT,
        category TEXT,
        year INTEGER,
        price REAL
    )
    ''')

    # بيانات الكتب (بالترتيب الجديد: category ثم year)
    books_data = [
        (1, 'Arabic', 'Arabic SD', 'Art/Science', 2001, 1.5),
        (2, 'French', 'French SD', 'Art', 2007, 1.5),
        (3, 'English', 'English SD', 'Art/Science', 2009, 1.5),
        (4, 'Mathematics', 'Mathematics SD', 'Science', 2013, 1.5),
        (5, 'Mathematics', 'Mathematics SD', 'Art', 2024, 2.5),
        (6, 'Principles of Geography and Economics', 'Social SD', 'Art', 2024, 2.5),
        (7, 'Psychology and sociology', 'Social SD', 'Art', 2024, 2.5),
        (8, 'Islamic History', 'Social SD', 'Art', 2024, 2.5),
        (9, 'Chemistry', 'Science SD', 'Science', 2013, 1.5),
        (10, 'Physics', 'Science SD', 'Science', 2013, 0.0),
        (11, 'Biology', 'Science SD', 'Science', 2013, 1.5),
        (12, 'Geology', 'Science SD', 'Science', 2013, 1.5),
        (13, 'ICT', 'ICT SD', 'Art/Science', 2025, 1.5),
        (14, 'Quran', 'Islamic SD', 'Art/Science', 2003, 1.5),
        (15, 'Islamic Studies', 'Islamic SD', 'Art/Science', 2013, 1.5)
    ]

    cursor1.executemany('INSERT INTO books VALUES (?,?,?,?,?,?)', books_data)
    conn1.commit()
    conn1.close()
    print("تم تحديث grade11_books.db بنجاح.")

    # --- 2. إعداد قاعدة بيانات المعالم (Kuwait_landmarks.db) ---
    print("\nجاري تهيئة قاعدة بيانات المعالم...")
    conn2 = sqlite3.connect(r'../Kuwait_landmarks.db')
    cursor2 = conn2.cursor()

    cursor2.execute('DROP TABLE IF EXISTS landmarks')

    # إنشاء الجداول بالأسماء الجديدة
    cursor2.execute('''
    CREATE TABLE landmarks (
        id INTEGER PRIMARY KEY,
        name TEXT,
        location TEXT,
        category TEXT,
        year INTEGER,
        price REAL
    )
    ''')


    # بيانات المعالم
    landmarks_data = [
        (1, 'Al-Tahrir Tower', 'Central Kuwait City', 'Architecture', 1996, 2.0),
        (2, 'Kuwait National Museum', 'Al-Murqab Area', 'Historical', 1983, 0.0),
        (3, 'The Avenues Mall', 'Al-Rai Area', 'Shopping', 2007, 0.0),
        (4, 'Failaka Island', 'Off the coast', 'Historical', 1950, 0.0),
        (5, 'Souq Al-Mubarakiya', 'Central Kuwait City', 'Traditional', 1897, 0.0),
        (6, 'Sheikh Jaber Bridge', 'Kuwait City', 'Modern', 2019, 0.0),
        (7, 'National Library', 'Al-Murqab Area', 'Cultural', 1994, 0.0),
        (8, 'Museum of Modern Art', 'Salmiya Area', 'Art', 1980, 1.0),
        (9, 'Cultural Center', 'Kuwait City', 'Cultural', 2016, 0.0),
        (10, 'Grand Mosque', 'Kuwait City', 'Religious', 1986, 0.0)
    ]



    cursor2.executemany('INSERT INTO landmarks VALUES (?,?,?,?,?,?)', landmarks_data)
    conn2.commit()
    conn2.close()
    print("تم تحديث Kuwait_landmarks.db بنجاح.")

if __name__ == "__main__":
    setup_databases()
    print("\nاكتملت جميع العمليات!")