import sqlite3


def setup_all_databases():
    # --- 1. إعداد قاعدة بيانات الكتب (grade11_books.db) ---
    print("جاري تهيئة قاعدة بيانات الكتب...")
    # الوصول للمجلد الأب باستخدام r'' لتجنب مشاكل المسارات في ويندوز
    conn_books = sqlite3.connect(r'../../grade11_books.db')
    cursor_books = conn_books.cursor()

    # حذف الجداول الحالية
    cursor_books.execute('DROP TABLE IF EXISTS books')

    # جعل category قبل year وحذف حقل cover
    cursor_books.execute('''
    CREATE TABLE books (
        id INTEGER PRIMARY KEY,
        title TEXT NOT NULL,
        author TEXT,
        category TEXT,
        year INTEGER,
        price REAL
    )
    ''')

    # بيانات الكتب بالترتيب المحدث
    books_data = [
        (1, 'Arabic', 'Arabic SD', 'Art/Science', 2001, 1.5),
        (2, 'French', 'French SD', 'Art', 2007, 1.5),
        (3, 'English', 'English SD', 'Art/Science', 2009, 1.5),
        (4, 'Mathematics', 'Mathematics SD', 'Science', 2013, 1.5),
        (5, 'Mathematics', 'Mathematics SD', 'Art', 2013, 1.5),
        (6, 'Principles of Geography and Economics', 'Social SD', 'Art', 2016, 1.5),
        (7, 'Psychology and sociology', 'Social SD', 'Art', 2016, 1.5),
        (8, 'Islamic History', 'Social SD', 'Art', 2007, 1.5),
        (9, 'Chemistry', 'Science SD', 'Science', 2013, 1.5),
        (10, 'Physics', 'Science SD', 'Science', 2013, 0.0),
        (11, 'Biology', 'Science SD', 'Science', 2013, 1.5),
        (12, 'Geology', 'Science SD', 'Science', 2013, 1.5),
        (13, 'ICT', 'ICT SD', 'Art/Science', 2025, 1.5),
        (14, 'Quran', 'Islamic SD', 'Art/Science', 2003, 1.5),
        (15, 'Islamic Studies', 'Islamic SD', 'Art/Science', 2013, 1.5)
    ]

    cursor_books.executemany('INSERT INTO books VALUES (?,?,?,?,?,?)', books_data)

    conn_books.commit()
    conn_books.close()
    print("تم تحديث grade11_books.db بنجاح.")

    # --- 2. إعداد قاعدة بيانات المعالم (Kuwait_landmarks.db) ---
    print("\nجاري تهيئة قاعدة بيانات المعالم...")
    conn_landmarks = sqlite3.connect(r'../Kuwait_landmarks.db')
    cursor_landmarks = conn_landmarks.cursor()

    cursor_landmarks.execute('DROP TABLE IF EXISTS landmarks')

    # إنشاء جدول المعالم
    cursor_landmarks.execute('''
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
        (1, 'Al-Tahrir Tower', 'Central Kuwait City', 'Architecture', 1996, 0.0),
        (2, 'Kuwait National Museum', 'Al-Murqab Area', 'Historical', 1983, 2.5),
        (3, 'The Avenues Mall', 'Al-Rai Area', 'Shopping', 2007, 0.0),
        (4, 'Failaka Island', 'Off the coast', 'Historical', 1950, 5.0),
        (5, 'Souq Al-Mubarakiya', 'Central Kuwait City', 'Traditional', 1897, 0.0),
        (6, 'Sheikh Jaber Bridge', 'Kuwait City', 'Modern', 2019, 0.0),
        (7, 'National Library', 'Al-Murqab Area', 'Cultural', 1994, 2.0),
        (8, 'Museum of Modern Art', 'Salmiya Area', 'Art', 1980, 1.0),
        (9, 'Cultural Center', 'Kuwait City', 'Cultural', 2016, 1.0),
        (10, 'Grand Mosque', 'Kuwait City', 'Religious', 1986, 0.0)
    ]


    cursor_landmarks.executemany('INSERT INTO landmarks VALUES (?,?,?,?,?,?)', landmarks_data)

    conn_landmarks.commit()
    conn_landmarks.close()
    print("تم تحديث Kuwait_landmarks.db بنجاح.")


if __name__ == "__main__":
    setup_all_databases()
    print("\nاكتملت جميع العمليات بنجاح!")