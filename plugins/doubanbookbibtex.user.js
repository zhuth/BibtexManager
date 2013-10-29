// ==UserScript==
// @name           douban book bibtex generator
// @description    douban book bibtex generator
// @namespace      http://tianhua.me/
// @auth           zhuth
// @version        0.1
// @license        Public Domain
// @include		   http://book.douban.com/subject/*
// ==/UserScript==

info = document.getElementById('info').innerText.split('\n');
field = { 作者 : 'author', 出版年 : 'year', 出版社 : 'publisher' };
data = {};
data['title'] = document.getElementById('wrapper').getElementsByTagName('h1')[0].innerText.trim();
for (i = 0; i < info.length; ++i) {
	li = info[i].split(':');
	if (li.length < 2) continue;
	li[0] = li[0].trim(); li[1] = li[1].trim();
	if (field[li[0]] == undefined) continue;
	data[field[li[0]]] = li[1];
}

l=window.location.pathname;
if (l[l.length-1]=='/') l = l.substring(0, l.length-1);
l = l.substring(l.lastIndexOf('/') + 1);

bibtex = 	"@book {book" + l + ",\r\n" + 
			"\tauthor = {" + data['author'] + "},\r\n" +
			"\ttitle = {" + data['title'] + "},\r\n" +
			"\tpublisher = {" + data['publisher'] + "},\r\n" +
			"\tyear = {" + data['year'] + "}\r\n" +
			"}\r\n";
bibtex = encodeURI("<textarea cols=50 rows=10>" + bibtex + "</textarea>");
			
document.getElementById('info').innerHTML += '<span class=p1><a href="#nogo" onclick="document.write(decodeURI(\'' + bibtex + '\'))">BibTex</a></span>';
