
$(document).ready(function () {

    //Recommend movies
    $("#recommendMe").click(function () {
        var userid = $("#userId").val();
        var table = $("#recomMovies").DataTable({
            ajax: {
                "bDestroy": true,
                url: "/api/Movies/recommendations/" + userid,
                dataSrc: ""
            },
            columns: [
                {
                    data: "title",
                    render: function (data, type, recommend) {
                        return "<a target='_blank' href='http://www.imdb.com/title/tt" + recommend.movieId + "'>" + recommend.title + "</a>";
                    }
                },

                {
                    data: "genre",
                    render: function (data) {
                        return data;
                    }
                },
                 {
                     data: "year",
                     render: function (data) {
                         return data;
                     }
                 }
            ]
        });
    })

    //Search movies
    $("#search").click(function () {
        var title = $("#title").val();
        var table = $("#moviesTable").DataTable({
            "bDestroy": true,
            ajax: {
                url: "/api/Movies/" + title,
                dataSrc: ""
            },
            columns: [
                {
                    data: "title",
                    render: function (data) {
                        return data;
                    }
                },

                {
                    data: "genre",
                    render: function (data) {
                        return data;
                    }
                },
                 {
                     data: "year",
                     render: function (data) {
                         return data;
                     }
                 }
            ]
        });
    })
});
