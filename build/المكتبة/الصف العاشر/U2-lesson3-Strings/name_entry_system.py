full_name = input('Enter your full name (Three Parts): ')
print('Characters in your name:',        (full_name))
position_space1 = full_name.find(' ')
position_space2 = full_name.find(' ', position_space1 + 1)
first_name = full_name[:               ]
second_name = full_name[position_space1 + 1:position_space2]
third_name =full_name [          :]
print(first_name.capitalize())
print(second_name.         )
print (third_name.         )